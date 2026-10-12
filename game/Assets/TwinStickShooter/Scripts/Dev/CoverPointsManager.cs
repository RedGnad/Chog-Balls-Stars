namespace TwinStickShooter
{
	using System.Collections.Generic;
	using UnityEngine;

	public class CoverPointsManager : MonoBehaviour
	{
		[SerializeField] private Vector2 _boundaries;

		public void GenerateCoverPoints()
		{
			ClearCoverPoints();

			List<GameObject> allCoverPoints = new List<GameObject>();

			var allGenerators = FindObjectsOfType<CoverPointsGenerator>();
			foreach (var generator in allGenerators)
			{
				generator.AddDummyCollider();
			}

			foreach (var generator in allGenerators)
			{
				allCoverPoints.AddRange(generator.Generate(_boundaries));
			}

			foreach (var generator in allGenerators)
			{
				generator.RemoveDummyCollider();
			}

			RemoveDuplicatedPoints(allCoverPoints);

			foreach (var point in allCoverPoints)
			{
				if (point != null)
				{
					point.transform.parent = this.transform;
				}
			}
		}

		public void ClearCoverPoints()
		{
			for (int i = transform.childCount - 1; i >= 0; i--)
			{
				DestroyImmediate(transform.GetChild(i).gameObject);
			}
		}

		private void RemoveDuplicatedPoints(List<GameObject> allPoints)
		{
			List<Vector3> usedPositions = new List<Vector3>();
			foreach (var point in allPoints)
			{
				if (usedPositions.Contains(point.transform.position) == true)
				{
					DestroyImmediate(point);
				}
				else
				{
					usedPositions.Add(point.transform.position);
				}
			}
		}
	}
}
