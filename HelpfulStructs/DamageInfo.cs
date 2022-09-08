using EFT;
using EFT.Ballistics;
using EFT.InventoryLogic;
using SIT.Tarkov.Core;
using SIT.Tarkov.Core.PlayerPatches.Health;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SIT.Coop.Core.HelpfulStructs
{
	//public struct DamageInfo
	//{
	//	public EDamageType DamageType;

	//	public float Damage;

	//	public float PenetrationPower;

	//	public Collider HitCollider;

	//	public Vector3 Direction;

	//	public Vector3 HitPoint;

	//	public Vector3 MasterOrigin;

	//	public Vector3 HitNormal;

	//	public BallisticCollider HittedBallisticCollider;

	//	//public EFT.Player Player;

	//	//public Item Weapon;

	//	public int FireIndex;

	//	public float ArmorDamage;

	//	public bool IsForwardHit;

	//	public float HeavyBleedingDelta;

	//	public float LightBleedingDelta;

	//	public string DeflectedBy;

	//	public string BlockedBy;

	//	public float StaminaBurnRate;

	//	public float DidBodyDamage;

	//	public float DidArmorDamage;

	//	public string SourceId;

	//	public EBodyPart? OverDamageFrom;

	//	public EBodyPartColliderType BodyPartColliderType;

	//	public bool Blunt
	//	{
	//		get
	//		{
	//			if (string.IsNullOrEmpty(this.DeflectedBy))
	//			{
	//				return !string.IsNullOrEmpty(this.BlockedBy);
	//			}
	//			return true;
	//		}
	//	}

	//	//public DamageInfo(EDamageType damageType,
	//	//2272 shot)
	//	//{
	//	//	this.DamageType = damageType;
	//	//	this.Damage = shot.Damage;
	//	//	this.PenetrationPower = shot.PenetrationPower;
	//	//	this.HitCollider = shot.HitCollider;
	//	//	this.Direction = shot.Direction;
	//	//	this.HitPoint = shot.HitPoint;
	//	//	this.HitNormal = shot.HitNormal;
	//	//	this.HittedBallisticCollider = shot.HittedBallisticCollider;
	//	//	this.Player = shot.Player;
	//	//	this.Weapon = shot.Weapon;
	//	//	this.FireIndex = shot.FireIndex;
	//	//	this.ArmorDamage = shot.ArmorDamage;
	//	//	this.DeflectedBy = shot.DeflectedBy;
	//	//	this.BlockedBy = shot.BlockedBy;
	//	//	this.MasterOrigin = shot.MasterOrigin;
	//	//	this.IsForwardHit = shot.IsForwardHit;
	//	//	this.SourceId = shot.Ammo.TemplateId;
	//	//          //if (shot.Ammo is GClass2077 gClass)
	//	//          //{
	//	//          //	this.StaminaBurnRate = gClass.StaminaBurnRate;
	//	//          //	this.HeavyBleedingDelta = gClass.HeavyBleedingDelta;
	//	//          //	this.LightBleedingDelta = gClass.LightBleedingDelta;
	//	//          //}
	//	//          //else
	//	//          //{
	//	//          float num = 0f;
	//	//          this.LightBleedingDelta = 0f;
	//	//          float heavyBleedingDelta = num;
	//	//          num = 0f;
	//	//          this.HeavyBleedingDelta = heavyBleedingDelta;
	//	//          this.StaminaBurnRate = num;
	//	//          //	if (this.Weapon is GClass2065 gClass2)
	//	//          //	{
	//	//          //		this.StaminaBurnRate = gClass2.KnifeComponent.Template.StaminaBurnRate;
	//	//          //	}
	//	//          //}
	//	//          this.DidBodyDamage = 0f;
	//	//	this.DidArmorDamage = 0f;
	//	//	this.OverDamageFrom = null;
	//	//	this.BodyPartColliderType = EBodyPartColliderType.None;
	//	//}

	//	public DamageInfo(Dictionary<string, object> dict)
 //       {
	//		this.DamageType = EDamageType.Bullet;
	//		this.Damage = 0;
 //           this.PenetrationPower = 0;
	//		this.HitCollider = null;
	//		this.Direction = Vector3.zero;
 //           this.HitPoint = Vector3.zero;
	//		this.HitNormal = Vector3.zero;
	//		this.HittedBallisticCollider = null;
 //           //this.Player = shot.Player;
 //           //this.Weapon = shot.Weapon;
 //           this.FireIndex = 0;
 //           this.ArmorDamage = 0;
	//		this.DeflectedBy = null;
	//		this.BlockedBy = null;
 //           this.MasterOrigin = Vector3.zero;
	//		this.IsForwardHit = false;
	//		this.SourceId = null; 
 //           //if (shot.Ammo is GClass2077 gClass)
 //           //{
 //           //	this.StaminaBurnRate = gClass.StaminaBurnRate;
 //           //	this.HeavyBleedingDelta = gClass.HeavyBleedingDelta;
 //           //	this.LightBleedingDelta = gClass.LightBleedingDelta;
 //           //}
 //           //else
 //           //{
 //           float num = 0f;
 //           this.LightBleedingDelta = 0f;
 //           float heavyBleedingDelta = num;
 //           num = 0f;
 //           this.HeavyBleedingDelta = heavyBleedingDelta;
 //           this.StaminaBurnRate = num;
 //           //	if (this.Weapon is GClass2065 gClass2)
 //           //	{
 //           //		this.StaminaBurnRate = gClass2.KnifeComponent.Template.StaminaBurnRate;
 //           //	}
 //           //}
 //           this.DidBodyDamage = 0f;
 //           this.DidArmorDamage = 0f;
 //           this.OverDamageFrom = null;
 //           this.BodyPartColliderType = EBodyPartColliderType.None;

	//		PatchConstants.ConvertDictionaryToObject(this, dict);
 //       }

	//	public DamageInfo GetOverDamage(EBodyPart sourceBodyPart)
	//	{
	//		DamageInfo result = this;
	//		result.OverDamageFrom = sourceBodyPart;
	//		return result;
	//	}

	//	public object ToNativeType()
 //       {
	//		var nativeType = Activator.CreateInstance(HealthControllerHelpers.GetDamageInfoType());
	//		return nativeType;
 //       }
	//}
}
