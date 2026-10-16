// Plays a recorded match again outside the game and says who won.
//
// The simulation is deterministic: the same build, the same configs and the same commands of the
// players give the same match on any machine. So a match does not have to be believed from what
// a player's page declares: its commands are replayed here, with the simulation code of the
// build, and the winner is read from what the simulation itself does. This is the path Photon
// documents for results that cannot be forged ("resimulation replays"), and what a stake is
// paid on.
//
//   Chog.Quantum.Verifier <replay.json | folder of a match> <asset database.json> <LUT folder> <report.json>
//
// The first argument is either the engine's replay file, as a game page records it
// (QuantumReplayFile), or the folder in which the game services keep what Photon's server sent
// of a match (server/src/matches.js): replay-start.json and replay-chunk-NNNNNN.json, the
// ReplayStart and ReplayChunk webhooks as received. The asset database is the one exported with
// the build (quantum-db.json); the LUT folder is game/Assets/Photon/Quantum/Resources/LUT.
// The report is written as JSON and the exit code is 0 when the match could be replayed to the
// end of its record, whoever won.

using System.Security.Cryptography;
using System.Text.Json;
using Photon.Deterministic;
using Quantum;
using Quantum.Json;

internal static unsafe class Verifier
{
    private static int Main(string[] args)
    {
        // A test of the rules, never a verdict: "--test-core-falls 1@900" destroys the core of
        // team 1 once tick 900 is reached, to see the match end for the other team. A report
        // made that way says so (testOptions).
        // "--test-seed 7" replays the record with another seed: the bots then play another match
        // against the same recorded commands, which is a quick way to watch what they do.
        // "--test-all-bots 1" hands the heroes of the recorded players to bots when the match
        // begins, as the game does for a player who left: six bots, three against three.
        // "--test-mode Siege" replays the record with the rules of another mode (MatchMode.qtn).
        (int team, int tick)? coreFalls = null;
        int? seed = null;
        EMatchMode? mode = null;
        var allBots = false;
        var usage = args.Length < 4 || args.Length % 2 != 0;
        for (var i = 4; i + 1 < args.Length && !usage; i += 2)
        {
            if (args[i] == "--test-core-falls")
            {
                var parts = args[i + 1].Split('@');
                coreFalls = (int.Parse(parts[0]), int.Parse(parts[1]));
            }
            else if (args[i] == "--test-seed")
                seed = int.Parse(args[i + 1]);
            else if (args[i] == "--test-all-bots" && args[i + 1] == "1")
                allBots = true;
            else if (args[i] == "--test-mode" && Enum.TryParse<EMatchMode>(args[i + 1], true, out var asked))
                mode = asked;
            else
                usage = true;
        }
        if (usage)
        {
            Console.Error.WriteLine("usage: Chog.Quantum.Verifier <replay.json | folder of a match> <asset database.json> <LUT folder> <report.json> [--test-core-falls <team>@<tick>] [--test-seed <number>] [--test-all-bots 1] [--test-mode TowersAndCoins|Siege|Coins]");
            return 2;
        }

        string recordPath = args[0], databasePath = args[1], lutPath = args[2], reportPath = args[3];
        Quantum.Log.InitializeForConsole(LogLevel.Warn);
        FPLut.Init(lutPath);

        var replay = Directory.Exists(recordPath)
            ? FromServerRecord(recordPath)
            : UnityJsonUtilityConvert.DeserializeObject<QuantumReplayFile>(File.ReadAllText(recordPath));
        var inputs = replay.CreateInputProvider();
        if (inputs == null)
        {
            Console.Error.WriteLine("the record holds no commands");
            return 1;
        }

        var serializer = new QuantumJsonSerializer();
        // A few rules of the sample's game manager are compiled by Unity into the game's own
        // assembly; here they are part of the simulation (tools/quantum/headless.py), and the
        // assets that name them are read accordingly.
        var database = File.ReadAllText(databasePath).Replace(", Assembly-CSharp\"", ", Quantum.Simulation\"");
        using var resources = new ResourceManagerStatic(serializer.AssetsFromByteArray(System.Text.Encoding.UTF8.GetBytes(database)),
            DotNetRunnerFactory.CreateNativeAllocator());
        var runtimeConfig = serializer.ConfigFromByteArray<RuntimeConfig>(replay.RuntimeConfigData.Decode(), compressed: true);
        var testedRules = seed != null || mode != null;
        if (seed != null)
            runtimeConfig.Seed = seed.Value;
        if (mode != null)
            runtimeConfig.Mode = mode.Value;

        // What the simulation says while it is replayed: the same events the game's own screens
        // react to. The match begins when the selection of characters ends, and from then on
        // nobody changes team.
        var events = new EventDispatcher();
        int? winnerTeam = null;
        var endedAtTick = 0;
        int? selectionEndedAtTick = null;
        events.SubscribeManual((EventGameOver e) =>
        {
            winnerTeam ??= e.WinnerTeam;
            if (endedAtTick == 0)
                endedAtTick = e.Tick;
        });
        events.SubscribeManual((EventFinishCharacterSelection e) => selectionEndedAtTick ??= e.Tick);
        // How often each team's tower hit a hero: a fact about the match, used to tune the towers.
        var towerShots = new int[2];
        events.SubscribeManual((EventStructureShot e) =>
        {
            if (e.Game.Frames.Verified.TryGet<TeamInfo>(e.Structure, out var owner) && owner.Index >= 0 && owner.Index < 2)
                towerShots[owner.Index]++;
        });
        // What fell, whose it was and when.
        var fallen = new List<object>();
        events.SubscribeManual((EventStructureDestroyed e) => fallen.Add(new { team = e.Team, kind = e.Kind.ToString(), tick = e.Tick }));

        var runner = SessionRunner.Start(new SessionRunner.Arguments
        {
            RunnerFactory = new DotNetRunnerFactory(),
            AssetSerializer = serializer,
            CallbackDispatcher = null,
            EventDispatcher = events,
            ResourceManager = resources,
            GameFlags = QuantumGameFlags.DisableInterpolatableStates,
            ReplayProvider = inputs,
            GameMode = DeterministicGameMode.Replay,
            SessionConfig = replay.DeterministicConfig,
            RuntimeConfig = runtimeConfig,
        });
        var game = (QuantumGame)runner.DeterministicGame;

        // As fast as the machine goes: nothing is drawn and nobody is waited for.
        var started = DateTime.UtcNow;
        List<object> playersAtStart = null;
        while (runner.Session.FramePredicted == null || runner.Session.FramePredicted.Number < replay.LastTick)
        {
            runner.Service(1.0f);
            if (playersAtStart == null && selectionEndedAtTick != null && game.Frames.Verified != null)
                playersAtStart = PlayersOf(game.Frames.Verified);
            if (allBots && selectionEndedAtTick != null && game.Frames.Verified != null)
            {
                var now = game.Frames.Verified;
                var heroes = now.Filter<Character, Bot>();
                while (heroes.NextUnsafe(out EntityRef entity, out Character* character, out Bot* bot))
                    if (bot->IsActive == false)
                        AISetupHelper.Botify(now, entity);
                allBots = false;
                testedRules = true;
            }
            if (coreFalls != null && game.Frames.Verified != null && game.Frames.Verified.Number >= coreFalls.Value.tick)
            {
                var now = game.Frames.Verified;
                var structures = now.Filter<Structure, TeamInfo>();
                while (structures.Next(out EntityRef entity, out Structure structure, out TeamInfo owner))
                    if (structure.Kind == EStructureKind.Core && owner.Index == coreFalls.Value.team)
                        Structures.Destroyed(now, entity);
                coreFalls = null;
                testedRules = true;
            }
            if ((DateTime.UtcNow - started).TotalSeconds > 300)
            {
                Console.Error.WriteLine("the replay did not reach its last tick in five minutes");
                runner.Shutdown();
                return 1;
            }
        }

        var frame = game.Frames.Verified;
        var teams = frame.ResolveList(frame.Global->TeamsData);
        var report = new
        {
            // Null when the record stops before anyone has won: nothing can be said of that match.
            winnerTeam,
            // The rules the match was played with: the coins picked up are the scores, or in a siege
            // the damage done to the other team's structures, in hundreds of health.
            mode = runtimeConfig.Mode.ToString(),
            scores = new[] { teams[0].Score, teams[1].Score },
            // The team whose core was destroyed, which ends a match whatever the scores; null
            // when both cores stood to the end.
            coreLostBy = frame.Global->DestroyedCore == 0 ? (int?)null : frame.Global->DestroyedCore - 1,
            // Hits by the tower of team 0 and by the tower of team 1.
            towerShots,
            // The health each structure had left when the replay stopped, and what fell at
            // which tick: facts about the match, used to tune the structures and the bots.
            structures = StructuresOf(frame),
            fallen,
            endedAtTick,
            selectionEndedAtTick,
            // The first tick of the record: with the pace, it places any tick in real time.
            firstTick = replay.LastTick - TicksIn(replay.InputHistoryDeltaCompressed.Decode()) + 1,
            lastTick = frame.Number,
            recordLastTick = replay.LastTick,
            updateRate = replay.DeterministicConfig.UpdateFPS,
            state = frame.Global->State.ToString(),
            // Who played in which team with which Genesis, taken when the match began: a player
            // who leaves later is still counted where it played.
            players = playersAtStart ?? PlayersOf(frame),
            // Options that only exist to test a match; one played with them is not a real one.
            testOptions = testedRules || runtimeConfig.StartWithFullSpecial || (runtimeConfig.BotGenesisTokens?.Length ?? 0) > 0,
            seconds = Math.Round((DateTime.UtcNow - started).TotalSeconds, 2),
            // Which simulation code replayed it: a report only binds the build it was made with.
            simulation = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(Frame).Assembly.Location))).ToLowerInvariant(),
        };
        runner.Shutdown();

        File.WriteAllText(reportPath, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }) + "\n");
        Console.WriteLine(winnerTeam == null
            ? $"no winner: the record ends at tick {frame.Number} in state {report.state}"
            : $"team {winnerTeam} won at tick {endedAtTick}, {report.scores[0]} to {report.scores[1]} ({frame.Number} ticks replayed in {report.seconds} s)");
        return 0;
    }

    private static List<object> StructuresOf(Frame frame)
    {
        var all = new List<object>();
        var structures = frame.Filter<Structure, TeamInfo>();
        while (structures.Next(out EntityRef entity, out Structure structure, out TeamInfo owner))
            all.Add(new
            {
                team = owner.Index,
                kind = structure.Kind.ToString(),
                health = AttributesHelper.GetAttributeData(frame, entity, EAttributeType.Health).CurrentValue.AsInt,
            });
        return all;
    }

    private static List<object> PlayersOf(Frame frame)
    {
        var players = new List<object>();
        var heroes = frame.Filter<PlayerLink, TeamInfo>();
        while (heroes.Next(out var entity, out var link, out var team))
        {
            // A character the machine plays has no player behind it.
            if (!link.PlayerRef.IsValid)
                continue;

            var tokenId = frame.TryGet<GenesisHero>(entity, out var hero) ? hero.TokenId : 0;
            players.Add(new { player = (int)link.PlayerRef, team = team.Index, tokenId });
        }

        return players;
    }

    // The input history is one block for each tick, each led by its length on four bytes.
    private static int TicksIn(byte[] inputHistory)
    {
        var ticks = 0;
        var at = 0;
        while (at + 4 <= inputHistory.Length)
        {
            at += 4 + BitConverter.ToInt32(inputHistory, at);
            ticks++;
        }

        if (at != inputHistory.Length)
            throw new InvalidDataException("the input history does not end where its last tick ends");

        return ticks;
    }

    // What Photon's server sent of a match, put together as a replay: the configs of the start
    // request and the slices of input in the order of their numbers.
    private static QuantumReplayFile FromServerRecord(string folder)
    {
        using var start = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "replay-start.json")));
        var input = new MemoryStream();
        var expected = 0;
        var lastTick = 0;
        var ended = false;
        foreach (var path in Directory.GetFiles(folder, "replay-chunk-*.json").OrderBy(name => name, StringComparer.Ordinal))
        {
            using var slice = JsonDocument.Parse(File.ReadAllText(path));
            var root = slice.RootElement;
            if (root.GetProperty("ChunkNumber").GetInt32() != expected++)
                throw new InvalidDataException("a slice of the input history is missing before " + Path.GetFileName(path));

            var bytes = Convert.FromBase64String(root.GetProperty("Input").GetString() ?? "");
            if (root.TryGetProperty("IsCompressed", out var compressed) && compressed.ValueKind == JsonValueKind.True)
                bytes = Compression.DecompressBytes(bytes);

            input.Write(bytes);
            lastTick = root.GetProperty("LastTick").GetInt32();
            ended = root.TryGetProperty("IsLast", out var last) && last.ValueKind == JsonValueKind.True;
        }

        if (!ended)
            throw new InvalidDataException("the input history has no last slice");

        return new QuantumReplayFile
        {
            DeterministicConfig = UnityJsonUtilityConvert.DeserializeObject<DeterministicSessionConfig>(start.RootElement.GetProperty("SessionConfig").GetRawText()),
            // The start request carries the config as the engine keeps it in a replay: gzipped JSON.
            RuntimeConfigData = new QuantumJsonFriendlyDataBlob { Base64 = start.RootElement.GetProperty("RuntimeConfig").GetString(), IsCompressed = false },
            InputHistoryDeltaCompressed = QuantumJsonFriendlyDataBlob.Encode(input.ToArray(), isCompressed: true, asBase64String: true),
            LastTick = lastTick,
        };
    }
}
