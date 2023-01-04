﻿using System;
using System.Collections.Generic;
using BaseNode;
using DijkstraSpace;
using Point_Map;
using Triangle_Map;
using UnityEngine;

namespace Agent_Space
{
    public class Agent
    {
        public string name { get; private set; }
        public List<MapNode> ocupedNodes { get; private set; }
        public float radius { get; private set; }

        public MapNode currentNode { get; private set; }
        public Point position { get; private set; }
        public PointNode currentPosition { get; private set; }
        private PointNode nextPosition;

        public Stack<MapNode> trianglePath { get; private set; }
        private Stack<PointNode> pointPath;
        public Point destination { get; private set; }

        public Stack<Point> visualPath { get; private set; }
        public bool inMove { get; /*private*/ set; }

        /// <summary> Compatibility of this Agent whit a material.</summary>
        public Dictionary<Material, float> compatibility;

        public Agent(float radius, string name = "agent")
        {
            this.name = name;
            compatibility = new Dictionary<Material, float>();
            trianglePath = new Stack<MapNode>();
            pointPath = new Stack<PointNode>();
            visualPath = new Stack<Point>();
            ocupedNodes = new List<MapNode>();
            this.radius = radius;
            inMove = false;
        }


        public void SetCompatibility(Material material, float value)
        {
            if (compatibility.ContainsKey(material))
                compatibility[material] = value;
            else
                compatibility.Add(material, value);
        }

        public void searchCurrentNode()
        {
            foreach (Node node in Environment.map.nodes)
                if ((node as MapNode).triangle.PointIn(position))
                {
                    currentNode = node as MapNode;
                    ocupedNodes.Add(currentNode);
                    currentNode.AddAgent(this);
                    SetOcupedFromPosition(2);
                    break;
                }
        }
        public void setPosition(Point point) { position = point; }

        public void SetOcupedFromPosition(float extendArea = 1)
        {
            Queue<MapNode> ocuped = new Queue<MapNode>();

            ocuped.Enqueue(currentNode);

            if (!ocupedNodes.Contains(currentNode))
            {
                ocupedNodes.Add(currentNode);
                currentNode.AddAgent(this);
            }

            for (int i = 0; i < ocupedNodes.Count; i++)
            {
                MapNode node = ocupedNodes[i];
                if (node != currentNode)
                {
                    if (position.DistanceToTriangle(node.triangle) > radius * extendArea)
                    {
                        node.RemoveAgent(this);
                        ocupedNodes.Remove(node);
                        i -= 1;
                    }
                    else
                        ocuped.Enqueue(node);
                }
            }

            while (ocuped.Count > 0)
            {
                MapNode node = ocuped.Dequeue();

                foreach (MapNode adj in node.adjacents.Keys)
                    if (position.DistanceToTriangle(adj.triangle) < radius + extendArea)
                        if (!ocupedNodes.Contains(adj))
                        {
                            ocuped.Enqueue(adj);
                            ocupedNodes.Add(adj);
                            adj.AddAgent(this);
                        }
            }
        }

        Tuple<MapNode[], MapNode, MapNode> LocalMap(Point endPoint)
        {
            return BFS(endPoint);
        }
        Tuple<MapNode[], MapNode, MapNode> BFS(Point endPoint)
        {
            bool endWasFound = false;
            int countAfterFound = Environment.bfsArea;

            List<MapNode> localMap = new List<MapNode>();

            Dictionary<MapNode, MapNode> r = new Dictionary<MapNode, MapNode>();
            List<MapNode> visited = new List<MapNode>();//Mejorar eficiencia de esto, no me gusta

            Queue<MapNode> q = new Queue<MapNode>();

            MapNode end = null;

            r.Add(currentNode, new MapNode(currentNode, this, endPoint));
            visited.Add(currentNode);

            localMap.Add(r[currentNode]);


            if (currentNode.triangle.PointIn(endPoint))
                ///If endPoint is in Current node, return only current node as path, init and endNode
                return new Tuple<MapNode[], MapNode, MapNode>(localMap.ToArray(), r[currentNode], r[currentNode]);

            q.Enqueue(currentNode);

            while (q.Count > 0)
            {
                if (endWasFound == true)
                {
                    countAfterFound--;
                    if (countAfterFound <= 0) break;
                }

                MapNode n = q.Dequeue();
                foreach (MapNode adj in n.adjacents.Keys)
                {
                    if (!visited.Contains(adj))
                    {
                        visited.Add(adj);
                        MapNode temp = new MapNode(adj, this, endPoint);

                        if (adj.triangle.PointIn(endPoint))
                        {
                            end = temp;
                            endWasFound = true;
                        }

                        r.Add(adj, temp);
                        r[n].AddAdjacent(temp, n.adjacents[adj]);
                        temp.AddAdjacent(r[n], n.adjacents[adj]);
                        //temp.SetDistance(Math.Min(temp.distance, r[n].distance + temp.Distance(n)));
                        q.Enqueue(adj);

                        localMap.Add(temp);
                    }
                    else
                    {
                        if (!r[n].adjacents.ContainsKey(r[adj]))
                        {
                            r[n].AddAdjacent(r[adj], n.adjacents[adj]);
                            r[adj].AddAdjacent(r[n], n.adjacents[adj]);
                            //r[adj].SetDistance(Math.Min(r[adj].distance, r[n].distance + r[adj].Distance(n)));
                        }
                    }
                }
            }

            if (end == null)
                localMap = new List<MapNode>();

            return new Tuple<MapNode[], MapNode, MapNode>(localMap.ToArray(), r[currentNode], end);
        }

        public MapNode[] GetTrianglePath(Point endPoint, bool push = true)
        {

            Tuple<MapNode[], MapNode, MapNode> localMap = LocalMap(endPoint);
            Node[] nodes = localMap.Item1;
            Node init = localMap.Item2;
            Node end = localMap.Item3;

            if (end == null)
                return null;

            Dijkstra dijkstra = new Dijkstra(init, end, nodes);

            MapNode[] result = tools.ToArrayAsMapNode(dijkstra.GetPath());

            if (push)
            {
                for (int i = result.Length - 1; i >= 0; i--)
                    trianglePath.Push(result[i]);
            }

            return result;
        }
        List<Arist> GetAritsPath(Point endPoint)
        {
            //Tuple<MapNode[], MapNode, MapNode> localMap = LocalMap(endPoint);
            //Node[] nodes = localMap.Item1; Node init = localMap.Item2; Node end = localMap.Item3;
            //Dijkstra dijkstra = new Dijkstra(init, end, nodes);

            //if (nodes.Length == 0)
            //    return null;

            MapNode[] path = GetTrianglePath(endPoint);
            if (path == null) return null;
            return Arist.ToAristList(path);
        }
        public PointNode[] GetPointPath(Point endPoint)
        {
            List<Arist> aritPath = GetAritsPath(endPoint);

            if (aritPath == null)
                return new PointNode[1] { new PointNode(position) };

            float density = Environment.densityPath;
            float mCost = currentNode.MaterialCost(this);
            List<PointNode> mapPoints = PointNode.Static.CreatePointMap(aritPath, position, endPoint, this, density, mCost);

            ///MapPoints[0] = initNode
            ///MapPoints[MapPoints.Count-1] = endNode
            Dijkstra dijkstra = new Dijkstra(mapPoints[0], mapPoints[mapPoints.Count - 1], mapPoints.ToArray());
            List<Node> pointPath = dijkstra.GetPath();

            return tools.ToArrayAsPointNode(pointPath);
        }
        public void SetPointPath(Point point)
        {
            trianglePath.Clear();
            destination = point;
            pointPath.Clear();
            //GetTrianglePath(point);
            PointNode[] path = GetPointPath(point);
            for (int i = path.Length - 1; i >= 0; i--)
                pointPath.Push(path[i]);

            if (pointPath.Count > 1)
            {
                currentPosition = pointPath.Pop();
                nextPosition = pointPath.Pop();
                NextPoint();
            }
            else
            {
                currentPosition = new PointNode(position);
                nextPosition = new PointNode(position);
                inMove = false;
            }
        }

        void DynamicSetPoint()
        {
            if (nextPosition.point.Distance(position) < 0.01f) return;

            float dist = radius * 3f;
            Point pointDest = position + Point.VectorUnit(position, nextPosition.point) * dist;

            Tuple<bool, Agent> collision = Collision(position, pointDest, this, ocupedNodes.ToArray(), 1.0f);
            if (collision.Item1)
            {
                if (pointPath.Count > 3)
                {
                    trianglePath.Pop();
                    trianglePath.Pop();

                    PointNode end = pointPath.Pop();

                    PointNode[] subPath = GetPointPath(end.point);

                    if (subPath.Length <= 1)
                    {
                        ///Not found Path
                        SetPointPath(destination);
                        return;
                    }

                    PointNode[] path = pointPath.ToArray();
                    subPath[subPath.Length - 1].AddAdjacent(path[0], end.adjacents[path[0]]);

                    for (int i = subPath.Length - 1; i >= 0; i--)
                        pointPath.Push(subPath[i]);

                    currentPosition = pointPath.Pop();
                    nextPosition = pointPath.Pop();
                    NextPoint();

                }
                else
                {
                    SetPointPath(destination);
                }
            }
        }

        int countMoves = 1;
        public void NextMove(int n = 1)
        {

            for (int i = 0; i < n; i++)
                NextMoveBasic();

            countMoves--;
            if (countMoves <= 0)
            {
                countMoves = 1;
                SetOcupedFromPosition(2);
            }
        }
        /// update freq = (frames/[speed / 5]) real frames .
        int frames = 10;
        void NextMoveBasic()
        {
            if (inMove)
            {
                if (visualPath.Count == 0) NextPoint();
                try { position = visualPath.Pop(); }
                catch { Debug.Log("Error: la pila tiene " + visualPath.Count + " elementos y esta intentando hacer Pop()."); }


                if (frames <= 0)
                {
                    frames = 10;
                    DynamicSetPoint();
                }
                frames--;
            }
        }
        void NextPoint()
        {
            visualPath.Clear();

            float cost = currentPosition.adjacents[nextPosition] * 25;
            List<Point> temp = new Arist(currentPosition.point, nextPosition.point).ToPoints(cost);
            for (int i = temp.Count - 1; i >= 0; i--)
                visualPath.Push(temp[i]);

            if (pointPath.Count == 0)
                inMove = false;
            else
            {
                currentPosition = nextPosition;
                nextPosition = pointPath.Pop();

                try { currentNode = trianglePath.Pop().origin; }
                catch { Debug.Log("La pila tiene " + trianglePath.Count + " elementos y esta intentando hacer push()"); }

                inMove = true;
            }
            NextMoveBasic();
        }

        public static Tuple<bool, Agent> Collision(Point node1, Point node2, Agent agent, MapNode mapNode, float multArea = 1)
        {
            Point l1 = node1;
            Point l2 = node2;
            float epsilon = 0.001f;
            foreach (Agent agentObstacle in mapNode.agentsIn)
            {
                if (agentObstacle == agent) continue;
                if (agentObstacle.position.DistanceToSegment(l1, l2) <= (agent.radius + agentObstacle.radius) * multArea + epsilon)
                    /// Collision
                    return new Tuple<bool, Agent>(true, agentObstacle);
            }
            return new Tuple<bool, Agent>(false, null);
        }
        public static Tuple<bool, Agent> Collision(Point node1, Point node2, Agent agent, MapNode[] mapNodes, float multArea = 1)
        {
            foreach (MapNode node in mapNodes)
            {
                Tuple<bool, Agent> collision = Collision(node1, node2, agent, node, multArea);
                if (collision.Item1)
                {
                    float distance = collision.Item2.position.Distance(agent.position, false);
                    float radius = collision.Item2.radius + agent.radius;
                    float epsilon = 0.01f;
                    if (distance <= radius + epsilon)
                    {
                        ///Choque
                        Point vector = Point.VectorUnit(collision.Item2.position, agent.position) * (radius - distance + epsilon);
                        agent.position = agent.position + vector;
                        collision.Item2.position = collision.Item2.position - vector;
                        //return new Tuple<bool, Agent>(false, null);
                    }
                    return collision;
                }
            }
            return new Tuple<bool, Agent>(false, null);
        }

        internal class tools
        {
            internal static MapNode[] ToArrayAsMapNode(List<Node> list)
            {
                MapNode[] result = new MapNode[list.Count];
                for (int i = 0; i < list.Count; i++)
                    result[i] = list[i] as MapNode;
                return result;
            }
            internal static PointNode[] ToArrayAsPointNode(List<Node> list)
            {
                PointNode[] result = new PointNode[list.Count];
                for (int i = 0; i < list.Count; i++)
                    result[i] = list[i] as PointNode;
                return result;
            }
        }
        public override string ToString()
        {
            return name;
        }
    }
}
