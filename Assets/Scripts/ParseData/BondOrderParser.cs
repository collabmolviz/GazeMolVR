using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Xml;
using System.Text;

namespace UMol {

public class BondOrderParser {

	/// Example of XML file parsed by this:
	/// <Bonds>
	/// <Bond from="0" to="1" order="2.0" type="covalent"/>
	/// <Bond from="0" to="2" order="2.0" type="covalent"/>
	/// <Bond from="0" to="3" order="2.0" type="covalent"/>
	/// </Bonds>


	public static Dictionary<bondOrderType, List<AtomDuo>> parseBondOrderFile(UnityMolModel m, string path) {
		Dictionary<AtomDuo, bondOrderType> covbondsOrder = new Dictionary<AtomDuo, bondOrderType>();
		Dictionary<bondOrderType, List<AtomDuo>> bondsperType = new Dictionary<bondOrderType, List<AtomDuo>>();
		List<bondWithOrder> tmpBO;

		try {
			StreamReader sr = new StreamReader(path);
			tmpBO = readBondOrder(sr);
		}
		catch (System.Exception e) {
			Debug.LogError("Couldn't read the bond order xml file " + e);
			return null;
		}

		StringBuilder debugsb = new StringBuilder();
		foreach (bondWithOrder bo in tmpBO) {
			if (bo.id >= 0 && bo.id < m.allAtoms.Count && bo.id2 >= 0 && bo.id2 < m.allAtoms.Count) {

				AtomDuo d = new AtomDuo(m.allAtoms[bo.id], m.allAtoms[bo.id2]);
				covbondsOrder[d] = bo.ordertype;
				if (!bondsperType.ContainsKey(bo.ordertype)) {
					bondsperType[bo.ordertype] = new List<AtomDuo>();
				}
				bondsperType[bo.ordertype].Add(d);
			}
			else {
				debugsb.Append("Ignoring bond between atoms ");
				debugsb.Append(bo.id);
				debugsb.Append(" and ");
				debugsb.Append(bo.id2);
				debugsb.Append("\n");
			}
		}
		if (debugsb.Length != 0) {
			Debug.LogWarning(debugsb.ToString());
		}
		m.covBondOrders = covbondsOrder;
		return bondsperType;
	}

	/// Read a xml file containing 2 atom id for each bond and a bond order
	public static List<bondWithOrder> readBondOrder(StreamReader sr) {
		List<bondWithOrder> result = new List<bondWithOrder>();
		using (sr)
		{
			XmlTextReader xmlR = new XmlTextReader(sr);
			while (xmlR.Read()) {

				if (xmlR.Name == "Bond") {

					bondWithOrder b;
					b.id = int.Parse(xmlR.GetAttribute("from"));
					b.id2 = int.Parse(xmlR.GetAttribute("to"));
					string o = xmlR.GetAttribute("order");
					string t = xmlR.GetAttribute("type");
					if (o == null) {
						b.ordertype.order = 1.0f;
					}
					else {
						b.ordertype.order = float.Parse(o);
					}

					if (t == null) { //Covalent by default
						b.ordertype.btype = bondType.covalent;
					}

					else {
						string[] tokens = t.Split(new [] { ',', ' '}, System.StringSplitOptions.RemoveEmptyEntries);
						foreach (string tok in tokens) {
							b.ordertype.btype = toBondType(tok);
							if (b.ordertype.btype != bondType.unknown)
								result.Add(b);
						}
						continue;
					}
					if (b.ordertype.btype != bondType.unknown)
						result.Add(b);
				}
			}
		}
		return result;
	}
	public struct bondWithOrder {
		public int id;
		public int id2;
		public bondOrderType ordertype;
	}
	public static bondType toBondType(string t) {
		if (t == null) {//Covalent by default
			return bondType.unknown;
		}
		string ts = t.Trim().ToLower();

		if (ts == "covalent" || ts == "db_geom") {
			return bondType.covalent;
		}
		else if (ts == "hbond" || ts == "h-bond" || ts == "hbond_weak") {
			return bondType.hbond;
		}
		else if (ts == "halogen") {
			return bondType.halogenbond;
		}
		else if (ts == "ionic") {
			return bondType.ionic;
		}
		else if (ts == "aromatic") {
			return bondType.aromatic;
		}
		else if (ts == "hydrophobic") {
			return bondType.hydrophobic;
		}
		else if (ts == "carbonyl") {
			return bondType.carbonyl;
		}
		return bondType.unknown;
	}

}
public struct bondOrderType {
	public float order;
	public bondType btype;
}

public enum bondType
{
	covalent,
	hbond,
	halogenbond,
	ionic,
	aromatic,
	hydrophobic,
	carbonyl,
	unknown //Used to discard this bond

}
}
