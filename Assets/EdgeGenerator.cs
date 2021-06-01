using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class EdgeGenerator : MonoBehaviour {
    [SerializeField] private int pointNum = 10;
    
    public List<Transform> points = new List<Transform>();
    [SerializeField] private bool preViewPints = true;
    [FormerlySerializedAs("previewEdge")] [SerializeField] private bool previewEdges = true;
    
    [ContextMenu("ResetPoints")]
    public void ResetPoints() {
        points.ForEach(trans => DestroyImmediate(trans.gameObject));
        points.Clear();
        for (var i = 0; i < pointNum; i++) {
            var rate = (float) i / pointNum;
            var theta = rate * Mathf.PI * 2;
            var s = Mathf.Sin(theta);
            var c = Mathf.Cos(theta);
            var pos = new Vector3(c, s, 0);
            var child = new GameObject(i.ToString());
            child.transform.position = pos;
            child.transform.SetParent(this.transform);
            points.Add(child.transform);
        }
    }
    public List<(Vector3, Vector3)> Edges() {
        var edges = new List<(Vector3, Vector3)>();
        for (var i = 0; i < points.Count; i++) {
            var nextId = (i + 1) % points.Count;
            var edge = (points[i].position, points[nextId].position);
            edges.Add(edge);
        }
        return edges;
    }

    [ContextMenu("Print Min Dist")]
    void PrintMinDist() {
        var m = Edges().Min(e => Vector3.Distance(e.Item1, e.Item2));
        Debug.Log(m);
    }

    private void OnDrawGizmos() {
        ShowPoints();
        ShowEdges();
    }

    void ShowPoints() {
        if(!preViewPints) return;
        if(points.Count == 0) return;
        points.ForEach(pos => {
            Gizmos.DrawSphere(pos.position, 0.05f);
        });
    }
    void ShowEdges() {
        if(!previewEdges) return;
        if(points.Count == 0) return;
        Edges().ForEach(edge => {
            Gizmos.DrawLine(edge.Item1, edge.Item2);
        });
    }
}