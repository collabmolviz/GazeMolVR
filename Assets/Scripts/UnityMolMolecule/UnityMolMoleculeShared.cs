using UnityEngine;
using System.Collections.Generic;


namespace UMol {
	
	public enum AtomType {
		noatom,
		sphere,
		optihb,
		cartoon,
		surface,
		DXSurface,
		fieldlines,
		trace,
		sugarribbons,
		sheherasade,
		ellipsoid,
		bondorder,
		point,
		explosurf
	}

	public enum BondType {
		nobond,
		line,
		optihs,
		hbond,
		hbondtube,
		bondorder
	}

	public enum FFType {
		atomic = 0,
		HiRERNA = 1
	}
	public struct RepType {
		public AtomType atomType;
		public BondType bondType;
	
		public static bool operator ==(RepType c1, RepType c2) 
		{
		    return c1.atomType.Equals(c2.atomType) && c1.bondType.Equals(c2.bondType);
		}

		public static bool operator !=(RepType c1, RepType c2) 
		{
		   return !(c1 == c2);
		}
		public override bool Equals(object obj){
			if (!(obj is RepType))
				return false;
			return ((RepType)obj) == this;
		}
		public override int GetHashCode(){
            int h = (int)atomType;
   			unchecked{
  
	   			const int factor = 9176;
	   			h = h * factor + (int)bondType;
   			}
            return h;
		}
	}



    public enum SurfMethod {
        EDTSurf = 0,
        MSMS = 1,
        QUICKSES = 2,
        QUICKSURF = 3
    }


}