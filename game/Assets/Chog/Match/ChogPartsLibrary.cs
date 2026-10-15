using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Chog.Match
{
    /// <summary>
    /// The library of 3D hero parts written by tools/blender/export_chog_parts.py (its file layout
    /// is described there): every part once, and for every Genesis of the base Origin the parts it
    /// wears and its skin. Plain data, read without the engine, so it is tested outside Unity;
    /// ChogHeroModel turns it into meshes.
    /// </summary>
    public sealed class ChogPartsLibrary
    {
        public const string Magic = "CHOGPARTS2";

        /// <summary>Where a material's colour comes from.</summary>
        public const byte RoleFixed = 0, RoleSkin = 1, RoleFace = 2;

        /// <summary>How a material is drawn.</summary>
        public const byte LightLit = 0, LightUnlit = 1, LightOutline = 2;

        public struct PartMaterial
        {
            public string Name;
            public float R, G, B;
            public byte Role;
            public byte Light;
        }

        public struct SkinColours
        {
            public string Name;
            public float SkinR, SkinG, SkinB;
            public float FaceR, FaceG, FaceB;
        }

        /// <summary>One of the four parts the game moves, and where it rests on its parent.</summary>
        public struct RestPart
        {
            public string Name;
            public string Parent;
            public float X, Y, Z;
        }

        public sealed class Piece
        {
            public string Name;
            /// <summary>The rest part it is drawn on, in whose axes its points are given.</summary>
            public string On;
            /// <summary>x, y, z of each vertex.</summary>
            public float[] Positions;
            /// <summary>x, y, z of each normal, from -127 to 127.</summary>
            public sbyte[] Normals;
            /// <summary>The material of each vertex, a position in Materials.</summary>
            public ushort[] Materials;
            /// <summary>Three vertices a triangle, already in Unity's winding.</summary>
            public int[] Indices;

            public int VertexCount => Materials.Length;
        }

        public struct Hero
        {
            public int Skin;
            public ushort[] Pieces;
        }

        public PartMaterial[] Materials;
        public SkinColours[] Skins;
        public RestPart[] Rest;
        public Piece[] Pieces;

        /// <summary>By the number shown in the name of the Genesis (its metadata id).</summary>
        public Dictionary<int, Hero> Heroes;

        public bool TryGetHero(int metadataId, out Hero hero)
        {
            hero = default;
            return Heroes != null && Heroes.TryGetValue(metadataId, out hero);
        }

        /// <summary>Reads the library; throws InvalidDataException on a file of another layout.</summary>
        public static ChogPartsLibrary Read(byte[] data)
        {
            if (data == null)
                throw new InvalidDataException("no library");

            using var reader = new BinaryReader(new MemoryStream(data), Encoding.UTF8);
            var magic = Encoding.ASCII.GetString(reader.ReadBytes(Magic.Length));
            if (magic != Magic)
                throw new InvalidDataException("not a library of hero parts: " + magic);

            var library = new ChogPartsLibrary();

            library.Materials = new PartMaterial[reader.ReadUInt32()];
            for (var i = 0; i < library.Materials.Length; i++)
            {
                library.Materials[i] = new PartMaterial
                {
                    Name = ReadString(reader),
                    R = reader.ReadSingle(), G = reader.ReadSingle(), B = reader.ReadSingle(),
                    Role = reader.ReadByte(), Light = reader.ReadByte()
                };
            }

            library.Skins = new SkinColours[reader.ReadUInt32()];
            for (var i = 0; i < library.Skins.Length; i++)
            {
                library.Skins[i] = new SkinColours
                {
                    Name = ReadString(reader),
                    SkinR = reader.ReadSingle(), SkinG = reader.ReadSingle(), SkinB = reader.ReadSingle(),
                    FaceR = reader.ReadSingle(), FaceG = reader.ReadSingle(), FaceB = reader.ReadSingle()
                };
            }

            library.Rest = new RestPart[reader.ReadUInt32()];
            for (var i = 0; i < library.Rest.Length; i++)
            {
                library.Rest[i] = new RestPart
                {
                    Name = ReadString(reader), Parent = ReadString(reader),
                    X = reader.ReadSingle(), Y = reader.ReadSingle(), Z = reader.ReadSingle()
                };
            }

            library.Pieces = new Piece[reader.ReadUInt32()];
            for (var i = 0; i < library.Pieces.Length; i++)
            {
                var piece = new Piece { Name = ReadString(reader), On = ReadString(reader) };
                var count = (int)reader.ReadUInt32();
                piece.Positions = new float[count * 3];
                for (var p = 0; p < piece.Positions.Length; p++)
                    piece.Positions[p] = reader.ReadSingle();
                piece.Normals = new sbyte[count * 3];
                for (var n = 0; n < piece.Normals.Length; n++)
                    piece.Normals[n] = reader.ReadSByte();
                piece.Materials = new ushort[count];
                for (var m = 0; m < count; m++)
                {
                    piece.Materials[m] = reader.ReadUInt16();
                    if (piece.Materials[m] >= library.Materials.Length)
                        throw new InvalidDataException(piece.Name + " names a material that is not in the library");
                }

                piece.Indices = new int[reader.ReadUInt32()];
                var wide = count > ushort.MaxValue;
                for (var t = 0; t < piece.Indices.Length; t++)
                {
                    piece.Indices[t] = wide ? (int)reader.ReadUInt32() : reader.ReadUInt16();
                    if (piece.Indices[t] >= count)
                        throw new InvalidDataException(piece.Name + " has a triangle outside its vertices");
                }

                library.Pieces[i] = piece;
            }

            var heroes = (int)reader.ReadUInt32();
            library.Heroes = new Dictionary<int, Hero>(heroes);
            for (var i = 0; i < heroes; i++)
            {
                var number = (int)reader.ReadUInt16();
                var hero = new Hero { Skin = reader.ReadByte(), Pieces = new ushort[reader.ReadByte()] };
                for (var p = 0; p < hero.Pieces.Length; p++)
                {
                    hero.Pieces[p] = reader.ReadUInt16();
                    if (hero.Pieces[p] >= library.Pieces.Length)
                        throw new InvalidDataException("CHOG #" + number + " wears a piece that is not in the library");
                }

                if (hero.Skin >= library.Skins.Length)
                    throw new InvalidDataException("CHOG #" + number + " has a skin that is not in the library");
                library.Heroes[number] = hero;
            }

            if (reader.BaseStream.Position != reader.BaseStream.Length)
                throw new InvalidDataException("the library goes on after its last Genesis");
            return library;
        }

        /// <summary>
        /// The colour of a vertex of a hero: r, g, b as on screen and how it is drawn in a (see the
        /// shader Chog/Hero: 1 lit, 0.6 unlit, 0.2 the outline).
        /// </summary>
        public void ColourOf(ushort material, int skin, out float r, out float g, out float b, out float a)
        {
            var made = Materials[material];
            var colours = Skins[skin];
            switch (made.Role)
            {
                case RoleSkin:
                    r = colours.SkinR; g = colours.SkinG; b = colours.SkinB;
                    break;
                case RoleFace:
                    r = colours.FaceR; g = colours.FaceG; b = colours.FaceB;
                    break;
                default:
                    r = made.R; g = made.G; b = made.B;
                    break;
            }

            a = LightOf(made.Light);
        }

        /// <summary>The alpha a vertex colour carries for each way of drawing (see the shader Chog/Hero).</summary>
        public static float LightOf(byte light)
        {
            switch (light)
            {
                case LightUnlit: return 0.6f;
                case LightOutline: return 0.2f;
                default: return 1f;
            }
        }

        private static string ReadString(BinaryReader reader)
        {
            var length = reader.ReadUInt16();
            return Encoding.UTF8.GetString(reader.ReadBytes(length));
        }
    }
}
