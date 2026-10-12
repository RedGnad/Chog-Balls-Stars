namespace TwinStickShooter
{
	using Quantum;
	using System.Collections.Generic;
	using UnityEngine;

	public class CoverPointsGenerator : MonoBehaviour
	{
		[SerializeField] private GameObject _coverPointPrefab;

		[SerializeField] private Quantum.CoverPoint _coverPointNorth;
		[SerializeField] private Quantum.CoverPoint _coverPointSouth;
		[SerializeField] private Quantum.CoverPoint _coverPointEast;
		[SerializeField] private Quantum.CoverPoint _coverPointWest;

		private BoxCollider _dummyCollider;

		public void AddDummyCollider()
		{
			var quantumCollider = GetComponentInChildren<QuantumStaticBoxCollider2D>();

			_dummyCollider = gameObject.AddComponent<BoxCollider>();
			var colliderSize = quantumCollider.Size.ToUnityVector2();
			_dummyCollider.size = new Vector3(colliderSize.x, 1, colliderSize.y);
			_dummyCollider.center = new Vector3(0, .5f, 0);
		}

		public void RemoveDummyCollider()
		{
			DestroyImmediate(_dummyCollider);
		}

		public List<GameObject> Generate(Vector3 boundaries)
		{
			var myCoverPoints = new List<GameObject>();

			GameObject newCoverPoint;

			Vector3 northPosition = transform.position + Vector3.forward;
			if (IsInsideBoundaries(boundaries, northPosition) && CheckCollision(northPosition) == false)
			{
				newCoverPoint = Instantiate(_coverPointPrefab, northPosition, Quaternion.identity);
				newCoverPoint.transform.eulerAngles = Vector3.zero;

				newCoverPoint.GetComponent<QuantumStaticCircleCollider2D>().Settings.Asset = _coverPointNorth;

				myCoverPoints.Add(newCoverPoint);
			}

			Vector3 southPosition = transform.position + Vector3.back;
			if (IsInsideBoundaries(boundaries, southPosition) && CheckCollision(southPosition) == false)
			{
				newCoverPoint = Instantiate(_coverPointPrefab, southPosition, Quaternion.identity);
				newCoverPoint.transform.eulerAngles = new Vector3(0, 180, 0);

				newCoverPoint.GetComponent<QuantumStaticCircleCollider2D>().Settings.Asset = _coverPointSouth;

				myCoverPoints.Add(newCoverPoint);
			}

			Vector3 westPosition = transform.position + Vector3.left;
			if (IsInsideBoundaries(boundaries, westPosition) && CheckCollision(westPosition) == false)
			{
				newCoverPoint = Instantiate(_coverPointPrefab, westPosition, Quaternion.identity);
				newCoverPoint.transform.eulerAngles = new Vector3(0, 270, 0);

				newCoverPoint.GetComponent<QuantumStaticCircleCollider2D>().Settings.Asset = _coverPointWest;

				myCoverPoints.Add(newCoverPoint);
			}

			Vector3 eastPosition = transform.position + Vector3.right;
			if (IsInsideBoundaries(boundaries, eastPosition) && CheckCollision(eastPosition) == false)
			{
				newCoverPoint = Instantiate(_coverPointPrefab, eastPosition, Quaternion.identity);
				newCoverPoint.transform.eulerAngles = new Vector3(0, 90, 0);

				newCoverPoint.GetComponent<QuantumStaticCircleCollider2D>().Settings.Asset = _coverPointEast;

				myCoverPoints.Add(newCoverPoint);
			}

			return myCoverPoints;
		}

		private bool CheckCollision(Vector3 position)
		{
			return Physics.OverlapSphere(position, 0.25f).Length > 0;
		}

		private bool IsInsideBoundaries(Vector3 boundaries, Vector3 position)
		{
			return position.x > -boundaries.x && position.x < boundaries.x
			                                  && position.z > -boundaries.y && position.z < boundaries.y;
		}
	}
}