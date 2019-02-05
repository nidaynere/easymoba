/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 16 February 2018
*/

using System.Collections.Generic;

public class Buffing
{ // Buffs and debuffs

    public enum buffTypes 
    {
        Stun,
        DamageShield,
        DamagerObject,
        BonusExperience,
        Poison,
        Healer,
        Speed
    };

    /*
    * BUFF TYPES;
    * 0=Stun // Decreases the move speed by percent.
    * 1=DamageShield // Absorbs the received damage.
    * 2=Damager object around user // Damages the closest agents around (closer than 2 meters) per second.
    * 3=Bonus Experience // Multiplies the received experience by percent.
    * 4=Poison // Damages you by the effect in per second.
    * 5=Healer object around user // Heals the closest agents around (closer than 4 meters) per second.
    * */

    [System.Serializable]
    public class Buff
    {
        public buffTypes buffType;
        public short buffEffect;
        public ushort buffTime;
    }

    public List<Buff> buffs;
}