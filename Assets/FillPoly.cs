using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FillPoly : MonoBehaviour {
    [SerializeField] private EdgeGenerator edgeGenerator;
    List<Vector3> tmp = new List<Vector3>();

    private void Update() {
        Fill();
    }

    [ContextMenu("Fill")]
    void Fill() {
        var sw = new Stopwatch();
        sw.Start();
        //boxing
        var edges = edgeGenerator.Edges().ToList();
        if (edges.Count == 0) {
            Debug.LogWarning("edge count equal 0.");
            return;
        }
        
        //make reference
        var vertToEdge = new Dictionary<Vector3, List<object>>();
        for (int i = 0; i < edges.Count; i++) {
            var edge = edges[i];
            AddOrDefault(vertToEdge, edge.Item1, edge);
            AddOrDefault(vertToEdge, edge.Item2, edge);
        }

        //make loop
        var loopSequence = MakeLoop(vertToEdge);
        tmp.Clear();
        tmp = loopSequence;
        
        var mesh = new Mesh();
        mesh.SetVertices(loopSequence);
        //原点から遠い順にインデックスを並べる
        var indices = new List<int>();
        
        var farOrderedIndices = loopSequence.Select((pos, i) => (pos.sqrMagnitude, i)).OrderByDescending(tuple => tuple.sqrMagnitude).Select(tuple => tuple.i).ToList();
        var removedIndices = new bool[loopSequence.Count];
        
        for (int i = 0; i < farOrderedIndices.Count-2; i++) {
            var id = farOrderedIndices[i];
            var prev = (id-1);
            if (prev < 0) prev = loopSequence.Count-1;
            while (removedIndices[prev]) {
                prev--;
                if (prev < 0) prev = loopSequence.Count-1;
            }
            var next = (id + 1) % loopSequence.Count;
            while (removedIndices[next]) next = (next+1) % loopSequence.Count;
            indices.Add(next);
            indices.Add(id);
            indices.Add(prev);
            removedIndices[id] = true;
        }
        mesh.SetIndices(indices, MeshTopology.Triangles, 0);
        mesh.RecalculateNormals();
        GetComponent<MeshFilter>().sharedMesh = mesh;
        sw.Stop();
        Debug.Log("Filling:"+((float)sw.ElapsedTicks/Stopwatch.Frequency)/1000 + "ms");
    }

    private List<Vector3> MakeLoop(Dictionary<Vector3, List<object>> vertToEdge) {
        var loopSequence = new List<Vector3>();
        var endPoints = vertToEdge.Keys.Take(1).ToList();
        {//init
            var end = endPoints[0];
            var sharedEdges = vertToEdge[end];
            endPoints.RemoveAt(0);
            for (int i = 0; i < 2; i++) {
                var edge = ((Vector3, Vector3)) sharedEdges[i];
                var another = edge.Item1 == end ? edge.Item2 : edge.Item1;
                endPoints.Add(another);
                loopSequence.Add(another);
                if(i==0) loopSequence.Add(end);
            }
        }
        while (endPoints.Count != 0) {
            var end = endPoints.Last();
            var sharedEdges = vertToEdge[end];
            endPoints.RemoveAt(endPoints.Count-1);//remove last
            
            var edge = ((Vector3, Vector3)) sharedEdges[0];
            var another = edge.Item1 == end ? edge.Item2 : edge.Item1;
            bool already = another == loopSequence[loopSequence.Count - 2];
            if (!already) {
                if (another == loopSequence.First()) break;
                loopSequence.Add(another);
            }
            else {
                edge = ((Vector3, Vector3)) sharedEdges[1];
                another = edge.Item1 == end ? edge.Item2 : edge.Item1;
                if (another == loopSequence.First()) break;
                loopSequence.Add(another);
            }
            endPoints.Add(another);
        }

        return loopSequence;
    }

    void AddOrDefault(Dictionary<Vector3, List<object>> dict, Vector3 key, object value) {
        var already = dict.TryGetValue(key, out var result);
        if (!already) {
            dict[key] = new List<object>{value};
        }
        else {
            result.Add(value);
        }
    }
    
    private void OnDrawGizmos() {
        //頂点が多いとLabel表示が重いので限定
        //if(Application.isPlaying) return;
        var range = Mathf.Min(10, tmp.Count); 
        for (var i = 0; i < range; i++) {   
            Handles.Label(tmp[i], i.ToString());
        }
        for (var i = tmp.Count-range; i < tmp.Count; i++) {
            Handles.Label(tmp[i], i.ToString());
        }
    }
}