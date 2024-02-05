using System;
using System.Collections;
using HQFPSTemplate.Pooling;
using UnityEngine;
using Random = UnityEngine.Random;

public class ProjecttileWeaponVFX : PlayerComponent, IEquipmentComponent
{
	#region Internal
#pragma warning disable 0649
	[Serializable]
	public class ParticleEffectsInfo
	{
		[BHeader("Particles")]

		public GameObject MuzzleFlashPrefab;

		[Space]

		public Vector3 MuzzleFlashOffset;
		public Vector3 MuzzleFlashRotOffset;
		public Vector2 MuzzleFlashRandomScale;
		public Vector3 MuzzleFlashRandomRot;

		[BHeader("Tracer")]

		public GameObject TracerPrefab;
		public Vector3 TracerOffset;
	}

	[Serializable]
	public class CasingEjectionInfo
	{
		public GameObject CasingPrefab;

		[Space]

		public float SpawnDelay;
		public float CasingScale = 1f;
		public float Spin;

		public Vector3 SpawnOffset;
		public Vector3 AimSpawnOffset;
		public Vector3 SpawnVelocity;
	}

	[Serializable]
	public class MagazineEjectionInfo
	{
		public GameObject MagazinePrefab;

		[Space]

		public float SpawnDelay;

		public Vector3 MagazineVelocity;
		public Vector3 MagazineAngularVelocity;
	}
	#endregion

	[Group] public ParticleEffectsInfo ParticleEffects = new();
	[Group] public CasingEjectionInfo CasingEjection;
	[Group] public MagazineEjectionInfo MagazineEjection;

	[SerializeField]
	private Transform muzzle;

	[SerializeField]
	private Transform casingEjectionPoint;

	[Space]

	[SerializeField]
	private LightEffect m_LightEffect;

	private ProjectileWeapon weapon;
	private WaitForSeconds casingSpawnDelay;

	public void Initialize(EquipmentItem equipmentItem)
	{
		weapon = equipmentItem as ProjectileWeapon;

		casingSpawnDelay = new WaitForSeconds(CasingEjection.SpawnDelay);

		// Create a pool for each gun effect, to help performance
		int minPoolSize = weapon.MagazineSize * 2;
		int maxPoolSize = minPoolSize * 2;

		if (ParticleEffects.MuzzleFlashPrefab != null)
			PoolingManager.Instance.CreatePool(ParticleEffects.MuzzleFlashPrefab, minPoolSize, maxPoolSize, true, ParticleEffects.MuzzleFlashPrefab.GetInstanceID().ToString(), 1f);

		if (ParticleEffects.TracerPrefab != null)
			PoolingManager.Instance.CreatePool(ParticleEffects.TracerPrefab, minPoolSize, maxPoolSize, true, ParticleEffects.TracerPrefab.GetInstanceID().ToString(), 3f);

		if (MagazineEjection.MagazinePrefab != null)
			PoolingManager.Instance.CreatePool(MagazineEjection.MagazinePrefab, 3, 10, true, MagazineEjection.MagazinePrefab.GetInstanceID().ToString(), 10f);

		if (CasingEjection.CasingPrefab != null)
			PoolingManager.Instance.CreatePool(CasingEjection.CasingPrefab, minPoolSize, maxPoolSize, true, CasingEjection.CasingPrefab.GetInstanceID().ToString(), 5f);
	}

	public void OnSelected()
	{
		weapon.FireHitPoints.AddListener(SpawnEffects);
		//Player.Reload.AddStartListener(SpawnMagazine);
	}

	private void OnDisable()
	{
		weapon.FireHitPoints.RemoveListener(SpawnEffects);
		//Player.Reload.RemoveStartListener(SpawnMagazine);
	}

	private void SpawnEffects(Vector3[] hitPoints)
	{
		if (!gameObject.activeSelf)
			return;

		if (muzzle != null)
		{
			// Create the bullet tracers if a prefab is assigned
			if (ParticleEffects.TracerPrefab)
			{
				for (int i = 0; i < hitPoints.Length; i++)
				{
					PoolingManager.Instance.GetObject(
						ParticleEffects.TracerPrefab,
						muzzle.position + muzzle.TransformVector(!Player.Aim.Active ? ParticleEffects.TracerOffset : Vector3.zero),
						Quaternion.LookRotation(hitPoints[i] - Player.Camera.transform.position)
					);
				}
			}

			// Muzzle flash
			if (ParticleEffects.MuzzleFlashPrefab != null)
			{
				var muzzleFlash = PoolingManager.Instance.GetObject(
					ParticleEffects.MuzzleFlashPrefab,
					Vector3.zero,
					Quaternion.identity,
					muzzle
				);
				//
				muzzleFlash.transform.localPosition = ParticleEffects.MuzzleFlashOffset;

				var randomMuzzleFlashRot = ParticleEffects.MuzzleFlashRandomRot;
				var MuzzleFlashRot = ParticleEffects.MuzzleFlashRotOffset;

				randomMuzzleFlashRot = new Vector3(
					Random.Range(-randomMuzzleFlashRot.x, randomMuzzleFlashRot.x),
					Random.Range(-randomMuzzleFlashRot.y, randomMuzzleFlashRot.y),
					Random.Range(-randomMuzzleFlashRot.z, randomMuzzleFlashRot.z));

				muzzleFlash.transform.localRotation = Quaternion.Euler(randomMuzzleFlashRot + MuzzleFlashRot);

				float randomMuzzleFlashScale = Random.Range(ParticleEffects.MuzzleFlashRandomScale.x, ParticleEffects.MuzzleFlashRandomScale.y);

				muzzleFlash.transform.localScale = new Vector3(randomMuzzleFlashScale, randomMuzzleFlashScale, randomMuzzleFlashScale);
			}
		}

		// Light
		if (m_LightEffect != null)
			m_LightEffect.Play(false);

		// Spawn the shell if a prefab is assigned
		if (CasingEjection.CasingPrefab != null && casingEjectionPoint != null)
			StartCoroutine(C_SpawnCasing());
	}

	private void OnValidate()
	{
		casingSpawnDelay = new WaitForSeconds(CasingEjection.SpawnDelay);
	}

	private IEnumerator C_SpawnCasing()
	{
		yield return casingSpawnDelay;

		Quaternion cassingSpawnRotation = Quaternion.Euler(Random.Range(-30, 30), Random.Range(-30, 30), Random.Range(-30, 30));
		Vector3 cassingSpawnPosition = casingEjectionPoint.position;

		var cassing = PoolingManager.Instance.GetObject(CasingEjection.CasingPrefab.gameObject, cassingSpawnPosition, cassingSpawnRotation);
		cassing.transform.localScale = new Vector3(CasingEjection.CasingScale, CasingEjection.CasingScale, CasingEjection.CasingScale);

		cassing.transform.localPosition = cassingSpawnPosition;

		var cassingRB = cassing.GetComponent<Rigidbody>();

		cassingRB.maxAngularVelocity = 10000f;

		cassingRB.velocity = transform.TransformVector(new Vector3(
			CasingEjection.SpawnVelocity.x * Random.Range(0.75f, 1.15f),
			CasingEjection.SpawnVelocity.y * Random.Range(0.9f, 1.1f),
			CasingEjection.SpawnVelocity.z * Random.Range(0.85f, 1.15f))) + Player.Velocity.Get();

		float spin = CasingEjection.Spin;

		cassingRB.angularVelocity = new Vector3(
			Random.Range(-spin, spin),
			Random.Range(-spin, spin),
			Random.Range(-spin, spin));
	}
}
