using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UMol.API;
using System;
using System.Linq;


namespace UMol {

public class SugarRibbons {

	public static float thick = 0.1f;
	public static float height = 0.1f;
	public static bool createPlanes = true;

	public static List<Mesh> createSugarRibbons(UnityMolSelection sel, int idFrame,
	        ref Dictionary<UnityMolAtom, List<int>> atomToVertId,
	        float rthickness = 0.1f, float rheight = 0.1f, bool createPlanes = true) {

		thick = rthickness;
		height = rheight;
		UMolGraph g = new UMolGraph();
		g.init(sel);
		List<List<UnityMolAtom>> cycles = g.getAllCycles();

		Mesh newMesh = new Mesh();
		Mesh bbMesh = new Mesh();
		newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
		bbMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

		List<Vector3> vertices = new List<Vector3>();
		List<Vector3> normals = new List<Vector3>();
		List<int> triangles = new List<int>();
		List<Color32> newColors = new List<Color32>();

		List<Vector3> verticesBB = new List<Vector3>();
		List<Vector3> normalsBB = new List<Vector3>();
		List<int> trianglesBB = new List<int>();
		List<Color32> newColorsBB = new List<Color32>();
		atomToVertId = new Dictionary<UnityMolAtom, List<int>>();

		for (int i = 0; i < cycles.Count; i++) {
			List<UnityMolAtom> c = cycles[i];

			Vector3 center = cog(c, sel, idFrame);
			Vector3 normal = computeMeanNormal(c, center, sel, idFrame);

			if (createPlanes) {
				constructCyclePlanes(c, center, normal, vertices, normals, triangles, newColors,
				                     sel, idFrame);
			}

			constructCycleBackbone(c, center, normal, verticesBB, normalsBB, trianglesBB, newColorsBB, atomToVertId,
			                       sel, idFrame);
		}
		for (int i = 0; i < cycles.Count - 1; i++) {
			for (int j = i + 1; j < cycles.Count; j++) {

				AtomDuo linknext = areCyclesLinked(g, cycles[i], cycles[j]);

				if (linknext != null && linknext.a1 != null && linknext.a2 != null) {

					constructLinkBetweenCycles(linknext.a1, linknext.a2, trianglesBB, atomToVertId);
				}
			}
		}


		newMesh.SetVertices(vertices);
		newMesh.SetTriangles(triangles, 0);
		newMesh.SetColors(newColors);
		newMesh.SetNormals(normals);
		// newMesh.RecalculateNormals();


		bbMesh.SetVertices(verticesBB);
		bbMesh.SetTriangles(trianglesBB, 0);
		bbMesh.SetColors(newColorsBB);
		bbMesh.SetNormals(normalsBB);
		// bbMesh.RecalculateNormals();


		List<Mesh> meshes = new List<Mesh>(2);

		meshes.Add(newMesh);
		meshes.Add(bbMesh);

		return meshes;

	}

	static void constructCyclePlanes(List<UnityMolAtom> cycleAtoms, Vector3 center, Vector3 normal,
	                                 List<Vector3> vertices, List<Vector3> normals, List<int> triangles,
	                                 List<Color32> colors,
	                                 UnityMolSelection sel, int idFrame) {

		if (cycleAtoms != null && cycleAtoms.Count > 2) {

			int idCenter = vertices.Count;
			vertices.Add(center);
			normals.Add(normal);
			colors.Add(Color.white);

			//Double face
			vertices.Add(center);
			normals.Add(-normal);
			colors.Add(Color.white);


			for (int i = 0; i < cycleAtoms.Count - 1; i++) {
				int id = vertices.Count;

				Vector3 curCycleAtomPos = cycleAtoms[i].position;
				Vector3 curCycleAtomPosP1 = cycleAtoms[i + 1].position;

				if (idFrame != -1) {
					int iida = sel.atomToIdInSel[cycleAtoms[i]];
					curCycleAtomPos = sel.extractTrajFramePositions[idFrame][iida];
					iida = sel.atomToIdInSel[cycleAtoms[i + 1]];
					curCycleAtomPosP1 = sel.extractTrajFramePositions[idFrame][iida];
				}

				vertices.Add(curCycleAtomPos);
				vertices.Add(curCycleAtomPosP1);

				normals.Add(normal);
				normals.Add(normal);
				colors.Add(Color.white);
				colors.Add(Color.white);

				triangles.Add(idCenter);
				triangles.Add(id);
				triangles.Add(id + 1);

				//Add the inverted triangle too

				vertices.Add(curCycleAtomPos);
				vertices.Add(curCycleAtomPosP1);

				normals.Add(-normal);
				normals.Add(-normal);
				colors.Add(Color.white);
				colors.Add(Color.white);

				triangles.Add(idCenter + 1);
				triangles.Add(id + 3);
				triangles.Add(id + 2);
			}

			int idlast = vertices.Count;

			Vector3 firstCycleAtomPos = cycleAtoms[0].position;
			Vector3 lastCycleAtomPos = cycleAtoms[cycleAtoms.Count - 1].position;


			if (idFrame != -1) {
				int iida = sel.atomToIdInSel[cycleAtoms[0]];
				firstCycleAtomPos = sel.extractTrajFramePositions[idFrame][iida];
				iida = sel.atomToIdInSel[cycleAtoms[cycleAtoms.Count - 1]];
				lastCycleAtomPos = sel.extractTrajFramePositions[idFrame][iida];
			}
			//Last triangle from last to 0
			vertices.Add(firstCycleAtomPos);
			vertices.Add(lastCycleAtomPos);

			normals.Add(normal);
			normals.Add(normal);
			colors.Add(Color.white);
			colors.Add(Color.white);

			triangles.Add(idCenter);
			triangles.Add(idlast);
			triangles.Add(idlast + 1);

			//Add the inverted triangle too
			vertices.Add(firstCycleAtomPos);
			vertices.Add(lastCycleAtomPos);

			normals.Add(-normal);
			normals.Add(-normal);
			colors.Add(Color.white);
			colors.Add(Color.white);

			triangles.Add(idCenter + 1);
			triangles.Add(idlast + 3);
			triangles.Add(idlast + 2);

		}
	}

	static void constructCycleBackbone(List<UnityMolAtom> cycleAtoms, Vector3 center, Vector3 normal,
	                                   List<Vector3> vertices, List<Vector3> normals, List<int> triangles,
	                                   List<Color32> colors, Dictionary<UnityMolAtom, List<int>> atomToVertId,
	                                   UnityMolSelection sel, int idFrame) {

		if (cycleAtoms != null && cycleAtoms.Count > 2) {
			int idVStart = vertices.Count;

			for (int i = 0; i < cycleAtoms.Count; i++) {

				UnityMolAtom aa2;
				Vector3 a1 = cycleAtoms[i].position;
				Vector3 a2 = Vector3.zero;

				if (idFrame != -1) {
					int iida = sel.atomToIdInSel[cycleAtoms[i]];
					a1 = sel.extractTrajFramePositions[idFrame][iida];
				}


				if (i == cycleAtoms.Count - 1) {
					a2 = cycleAtoms[0].position;
					aa2 = cycleAtoms[0];
					if (idFrame != -1) {
						int iida = sel.atomToIdInSel[cycleAtoms[0]];
						a2 = sel.extractTrajFramePositions[idFrame][iida];
					}

				}
				else {
					a2 = cycleAtoms[i + 1].position;
					aa2 = cycleAtoms[i + 1];
					if (idFrame != -1) {
						int iida = sel.atomToIdInSel[cycleAtoms[i + 1]];
						a2 = sel.extractTrajFramePositions[idFrame][iida];
					}
				}

				Vector3 a1Toa2 = a2 - a1;
				Vector3 normalToa1a2 = Vector3.Cross(a1Toa2, normal).normalized;
				int idV = vertices.Count;
				//out-up
				vertices.Add(a1 + (thick * normalToa1a2) + normal * height);
				//out-down
				vertices.Add(a1 + (thick * normalToa1a2) + -normal * height);

				//in-up
				vertices.Add(a1 - (thick * normalToa1a2) + normal * height);
				//in-down
				vertices.Add(a1 - (thick * normalToa1a2) + -normal * height);

				//out-up
				vertices.Add(a2 + (thick * normalToa1a2) + normal * height);
				//out-down
				vertices.Add(a2 + (thick * normalToa1a2) + -normal * height);

				//in-up
				vertices.Add(a2 - (thick * normalToa1a2) + normal * height);
				//in-down
				vertices.Add(a2 - (thick * normalToa1a2) + -normal * height);


				if (!atomToVertId.ContainsKey(cycleAtoms[i])) {
					atomToVertId[cycleAtoms[i]] = new List<int>();
				}
				atomToVertId[cycleAtoms[i]].Add(idV);
				atomToVertId[cycleAtoms[i]].Add(idV + 1);
				atomToVertId[cycleAtoms[i]].Add(idV + 2);
				atomToVertId[cycleAtoms[i]].Add(idV + 3);

				if (!atomToVertId.ContainsKey(aa2)) {
					atomToVertId[aa2] = new List<int>();
				}
				atomToVertId[aa2].Add(idV + 4);
				atomToVertId[aa2].Add(idV + 5);
				atomToVertId[aa2].Add(idV + 6);
				atomToVertId[aa2].Add(idV + 7);

				normals.Add((normal + normalToa1a2) * 0.5f);
				normals.Add((-normal + normalToa1a2) * 0.5f);

				normals.Add((normal - normalToa1a2) * 0.5f);
				normals.Add((-normal - normalToa1a2) * 0.5f);

				normals.Add((normal + normalToa1a2) * 0.5f);
				normals.Add((-normal + normalToa1a2) * 0.5f);

				normals.Add((normal - normalToa1a2) * 0.5f);
				normals.Add((-normal - normalToa1a2) * 0.5f);

				colors.Add(cycleAtoms[i].color);
				colors.Add(cycleAtoms[i].color);

				colors.Add(cycleAtoms[i].color);
				colors.Add(cycleAtoms[i].color);

				colors.Add(aa2.color);
				colors.Add(aa2.color);

				colors.Add(aa2.color);
				colors.Add(aa2.color);

				triangles.Add(idV);
				triangles.Add(idV + 1);
				triangles.Add(idV + 5);

				triangles.Add(idV);
				triangles.Add(idV + 5);
				triangles.Add(idV + 4);

				triangles.Add(idV + 3);
				triangles.Add(idV + 2);
				triangles.Add(idV + 7);

				triangles.Add(idV + 7);
				triangles.Add(idV + 2);
				triangles.Add(idV + 6);

				//Up
				triangles.Add(idV);
				triangles.Add(idV + 6);
				triangles.Add(idV + 2);

				triangles.Add(idV);
				triangles.Add(idV + 4);
				triangles.Add(idV + 6);


				//Down
				triangles.Add(idV + 1);
				triangles.Add(idV + 3);
				triangles.Add(idV + 7);

				triangles.Add(idV + 1);
				triangles.Add(idV + 7);
				triangles.Add(idV + 5);


				if (i < cycleAtoms.Count - 1) {
					//Out Close the rectangle with in between points
					triangles.Add(idV + 4);
					triangles.Add(idV + 5);
					triangles.Add(idV + 9);

					triangles.Add(idV + 4);
					triangles.Add(idV + 9);
					triangles.Add(idV + 8);

					//In
					triangles.Add(idV + 7);
					triangles.Add(idV + 6);
					triangles.Add(idV + 11);

					triangles.Add(idV + 6);
					triangles.Add(idV + 10);
					triangles.Add(idV + 11);

					//Up
					triangles.Add(idV + 4);
					triangles.Add(idV + 10);
					triangles.Add(idV + 6);

					triangles.Add(idV + 4);
					triangles.Add(idV + 8);
					triangles.Add(idV + 10);

					//Down
					triangles.Add(idV + 5);
					triangles.Add(idV + 7);
					triangles.Add(idV + 11);

					triangles.Add(idV + 5);
					triangles.Add(idV + 11);
					triangles.Add(idV + 9);

				}
				else {
					triangles.Add(idV + 4);
					triangles.Add(idV + 5);
					triangles.Add(idVStart + 1);

					triangles.Add(idV + 4);
					triangles.Add(idVStart + 1);
					triangles.Add(idVStart + 0);

					//In
					triangles.Add(idV + 7);
					triangles.Add(idV + 6);
					triangles.Add(idVStart + 3);

					triangles.Add(idV + 6);
					triangles.Add(idVStart + 2);
					triangles.Add(idVStart + 3);

					//Up
					triangles.Add(idV + 4);
					triangles.Add(idVStart + 2);
					triangles.Add(idV + 6);

					triangles.Add(idV + 4);
					triangles.Add(idVStart + 0);
					triangles.Add(idVStart + 2);

					//Down
					triangles.Add(idV + 5);
					triangles.Add(idV + 7);
					triangles.Add(idVStart + 3);

					triangles.Add(idV + 5);
					triangles.Add(idVStart + 3);
					triangles.Add(idVStart + 1);

				}

			}
		}

	}

	static void constructLinkBetweenCycles(UnityMolAtom a1, UnityMolAtom a2,
	                                       List<int> triangles,
	                                       Dictionary<UnityMolAtom, List<int>> atomToVertId) {


		List<int> vertA1 = atomToVertId[a1];
		List<int> vertA2 = atomToVertId[a2];


		triangles.Add(vertA1[0]);
		triangles.Add(vertA1[1]);
		triangles.Add(vertA2[0]);

		triangles.Add(vertA1[1]);
		triangles.Add(vertA2[1]);
		triangles.Add(vertA2[0]);


		triangles.Add(vertA1[4]);
		triangles.Add(vertA2[4]);
		triangles.Add(vertA1[5]);

		triangles.Add(vertA2[4]);
		triangles.Add(vertA2[5]);
		triangles.Add(vertA1[5]);


		triangles.Add(vertA1[4]);
		triangles.Add(vertA1[0]);
		triangles.Add(vertA2[0]);

		triangles.Add(vertA2[0]);
		triangles.Add(vertA2[4]);
		triangles.Add(vertA1[4]);


		triangles.Add(vertA1[1]);
		triangles.Add(vertA1[5]);
		triangles.Add(vertA2[1]);

		triangles.Add(vertA1[5]);
		triangles.Add(vertA2[5]);
		triangles.Add(vertA2[1]);

	}

	static AtomDuo areCyclesLinked(UMolGraph g, List<UnityMolAtom> c1, List<UnityMolAtom> c2, int maxDistLink = 3) {
		AtomDuo res = null;

		//One atom of c1 is part of the same connected component as one atom from the cycle c2
		int segc1id = g.getSegmentId(c1[0]);
		int segc2id = g.getSegmentId(c2[0]);

		if (segc1id == segc2id) {
			//Get one path from one atom of the cycle c1 to one atom of the cycle c2
			List<UnityMolAtom> path = g.getPath(segc1id, c1[0], c2[0]);
			UnityMolAtom lastc2 = null;
			UnityMolAtom firstc1 = null;
			int d = 0;
			foreach (UnityMolAtom a in path) {
				if (c2.Contains(a)) {
					lastc2 = a;
				}
				else if (c1.Contains(a)) {
					firstc1 = a;
					break;
				}
				else {
					d++;
				}
			}
			if (d > maxDistLink)
				return res;
			res = new AtomDuo(firstc1, lastc2);
		}
		return res;
	}


	static Vector3 cog(List<UnityMolAtom> cycleAtoms,
	                   UnityMolSelection sel, int idFrame) {
		Vector3 p = Vector3.zero;

		foreach (UnityMolAtom a in cycleAtoms) {
			if (idFrame != -1) {
				int iida = sel.atomToIdInSel[a];
				p += sel.extractTrajFramePositions[idFrame][iida];
			}
			else {
				p += a.position;
			}
		}
		return p / Mathf.Max(1, cycleAtoms.Count);
	}

	static Vector3 computeMeanNormal(List<UnityMolAtom> cycleAtoms, Vector3 c,
	                                 UnityMolSelection sel, int idFrame) {

		Vector3 n = Vector3.zero;
		for (int i = 0; i < cycleAtoms.Count - 1; i++) {
			Vector3 cToa1 = cycleAtoms[i].position - c;
			Vector3 cToa2 = cycleAtoms[i + 1].position - c;
			
			if (idFrame != -1) {
				int iida = sel.atomToIdInSel[cycleAtoms[i]];
				cToa1 = sel.extractTrajFramePositions[idFrame][iida] - c;
				iida = sel.atomToIdInSel[cycleAtoms[i + 1]];
				cToa2 = sel.extractTrajFramePositions[idFrame][iida] - c;
			}

			n += Vector3.Cross(cToa1, cToa2);

			if (i == cycleAtoms.Count - 1) {
				cToa1 = cycleAtoms[i].position - c;
				cToa2 = cycleAtoms[0].position - c;

				if (idFrame != -1) {
					int iida = sel.atomToIdInSel[cycleAtoms[i]];
					cToa1 = sel.extractTrajFramePositions[idFrame][iida] - c;
					iida = sel.atomToIdInSel[cycleAtoms[0]];
					cToa2 = sel.extractTrajFramePositions[idFrame][iida] - c;
				}

				n += Vector3.Cross(cToa1, cToa2);
			}
		}

		return (n / Mathf.Max(1, cycleAtoms.Count)).normalized;
	}



	public class UMolGraph {

		public List<List<UnityMolAtom>> segments = null;
		int nbCycles = 0;
		Dictionary<UnityMolAtom, GNode> dicNode;
		UnityMolSelection selection;
		List<UnityMolAtom> curList = new List<UnityMolAtom>();


		public void init(UnityMolSelection sel) {
			selection = sel;
		}

		void findAllCycles(UnityMolAtom u, UnityMolAtom p) {

			// Already completely visited
			if (dicNode.ContainsKey(u) && dicNode[u].nodeCol == 2) {
				return;
			}

			// seen vertex, but was not completely visited -> cycle detected.
			// backtrack based on parents to find the complete cycle.
			if (dicNode.ContainsKey(u) && dicNode[u].nodeCol == 1) {
				nbCycles++;
				UnityMolAtom cur = p;
				dicNode[cur].nodeMark = nbCycles;

				// backtrack the vertex which are
				// in the current cycle thats found
				while (cur != u) {
					cur = dicNode[cur].nodePar;
					dicNode[cur].nodeMark = nbCycles;
				}
				return;
			}

			if (!dicNode.ContainsKey(u)) {
				dicNode[u] = new GNode();
			}
			dicNode[u].nodePar = p;
			// partially visited.
			dicNode[u].nodeCol = 1;

			// simple dfs on graph
			int[] res = null;
			if(selection.bonds.bonds.TryGetValue(u.idInAllAtoms, out res)){
				foreach (int idv in res) {
					if (idv != -1) {
						UnityMolAtom v = u.residue.chain.model.allAtoms[idv];
						if (v == dicNode[u].nodePar) {
							continue;
						}
						findAllCycles(v, u);
					}
				}
			}

			dicNode[u].nodeCol = 2;
		}

		public List<List<UnityMolAtom>> getAllCycles() {

			//Search connected components to make sure we find all the cycles even in seperated chains
			if (segments == null) {
				segments = getConnectedCompo();
			}

			dicNode = new Dictionary<UnityMolAtom, GNode>();


			foreach (List<UnityMolAtom> ats in segments) {
				AtomDuo first = getFirstBond(ats);
				if (first != null) {
					findAllCycles(first.a1, first.a2);
				}
			}

			List<List<UnityMolAtom>> resCycles = new List<List<UnityMolAtom>>();
			Dictionary<int, int> markToListId = new Dictionary<int, int>();

			foreach (UnityMolAtom a in dicNode.Keys) {
				int mark = dicNode[a].nodeMark;
				if (mark > 0) {
					if (markToListId.ContainsKey(mark)) {
						resCycles[markToListId[mark]].Add(a);
					}
					else {
						int id = resCycles.Count;
						markToListId[mark] = id;
						List<UnityMolAtom> newC = new List<UnityMolAtom>();
						newC.Add(a);
						resCycles.Add(newC);
					}
				}
			}

			List<List<UnityMolAtom>> resCyclesfilter = new List<List<UnityMolAtom>>();
			foreach (List<UnityMolAtom> c in resCycles) {
				if (c.Count >= 3) {
					resCyclesfilter.Add(c);
				}
			}


			return resCyclesfilter;
		}
		public int getSegmentId(UnityMolAtom a) {
			int id = 0;
			foreach (List<UnityMolAtom> s in segments) {
				if (s.Contains(a)) {
					return id;
				}
				id++;
			}
			return -1;
		}

		AtomDuo getFirstBond(List<UnityMolAtom> atoms) {
			if (atoms.Count < 3) {
				return null;
			}

			int[] res = null;
			int[] res2 = null;
			UnityMolModel curM = atoms[0].residue.chain.model;
			foreach (UnityMolAtom firstA in atoms) {
				if(selection.bonds.bonds.TryGetValue(firstA.idInAllAtoms, out res)){
					foreach (int ida in res) {
						if (ida != -1 && selection.bonds.bonds.TryGetValue(ida, out res2)) {
							UnityMolAtom a = curM.allAtoms[ida];
							foreach (int idb in res2) {
								if (idb != -1) {
									UnityMolAtom b = curM.allAtoms[idb];
									int count = selection.bonds.countBondedAtoms(a);
									if (count > 1) {
										return new AtomDuo(a, b);
									}
								}
							}
						}
					}
				}
			}
			return null;
		}

		List<List<UnityMolAtom>> getConnectedCompo() {
			List<List<UnityMolAtom>> res = new List<List<UnityMolAtom>>();
			HashSet<UnityMolAtom> visited = new HashSet<UnityMolAtom>();

			foreach (UnityMolAtom a in selection.atoms) {

				if (!visited.Contains(a)) {
					DFSUtil(a, visited);
					if (curList.Count > 1) {
						res.Add(new List<UnityMolAtom>(curList));
					}
					curList.Clear();
				}
			}

			return res;
		}

		void DFSUtil(UnityMolAtom v, HashSet<UnityMolAtom> visited) {
			visited.Add(v);
			curList.Add(v);

			int[] res = null;
			if(selection.bonds.bonds.TryGetValue(v.idInAllAtoms, out res)){
				foreach (int idx in res) {
					if (idx != -1){
						UnityMolAtom x = v.residue.chain.model.allAtoms[idx];
						if(!visited.Contains(x)) {
							DFSUtil(x, visited);
						}
					}
				}
			}
		}

		// Assumes that start and stop are in the segment segId
		public List<UnityMolAtom> getPath(int segId, UnityMolAtom start, UnityMolAtom stop) {
			HashSet<UnityMolAtom> visited = new HashSet<UnityMolAtom>();

			if (segId < 0 || segId >= segments.Count) {
				return null;
			}

			List<UnityMolAtom> res = new List<UnityMolAtom>();
			List<UnityMolAtom> q = new List<UnityMolAtom>();
			Dictionary<UnityMolAtom, UnityMolAtom> pred = new Dictionary<UnityMolAtom, UnityMolAtom>();
			visited.Add(start);
			q.Add(start);

			bool found = false;
			while (q.Count != 0) {
				if (found) {
					break;
				}
				UnityMolAtom a = q.First();
				q.RemoveAt(0);
				
				int[] bonded = null;
				if(selection.bonds.bonds.TryGetValue(a.idInAllAtoms, out bonded)){
					foreach(int idb in bonded){
						if (idb != -1){
							UnityMolAtom b = start.residue.chain.model.allAtoms[idb];
							if(!visited.Contains(b)) {
								visited.Add(b);
								pred[b] = a;
								q.Add(b);

								if (b == stop) {
									found = true;
									break;
								}
							}
						}
					}
				}
			}

			if (found) {
				res.Add(stop);
				UnityMolAtom a = stop;
				while (a != start) {
					res.Add(pred[a]);
					a = pred[a];
				}
				return res;
			}

			return null;
		}

		public class GNode {
			public int nodeCol = -1;
			public int nodeMark = -1;
			public UnityMolAtom nodePar = null;
		}

	}
}
}