using System;
using UnityEngine;
namespace BahaTurret
{
	public interface IBDWeapon 
	{
		WeaponClasses GetWeaponClass();
		string GetShortName();
		string GetSubLabel();
		Part GetPart();
	}

	public enum WeaponClasses{Missile, Bomb, Gun, Rocket, DefenseLaser}
}

