using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Burst;

namespace FinalProject
{
    public class Manager : MonoBehaviour
    {
        ////////////////////////////////////////////////////////////// Structure and Class Definitions//////////////////////////////////////////////////////
        public struct MyNodes // Define Node structure
        {
            public float x, y;
            //public int isUsable;
            public int ID;

            public MyNodes(float a, float b, int id)
            {
                x = a;
                y = b;
                //isUsable = 1;
                ID = id;
            }
            //public void setUsability(int i)
            //{
            //    isUsable = i;
            //}
        }

        public class MyEdges  // Define Edge Class(I named them as MyEdges and MyNodes in order to not interfere with any potential keywords)
        {
            public MyNodes A, B;
            public float weight;
            public MyEdges(MyNodes a, MyNodes b, float w)
            {
                weight = w;
                A = a;
                B = b;
            }
        }

        public class Graph // Defined a basic graph containing a lists of edges and Nodes. 
        {
            public List<MyNodes> Nodelist = new List<MyNodes>();
            public List<MyEdges> Edgelist = new List<MyEdges>();
        }

        public class NodeValue // Defined this class because there is no multi-value dictionary for a single key
        {
            public MyNodes prevNode;
            public float Cost;

            public NodeValue(MyNodes prevnode, float cost)
            {
                prevNode = prevnode;
                Cost = cost;
            }
        }
        /////////////////////////////////////////////////////////////////  Declarations  /////////////////////////////////////////////////////////////////////
        private float x, y;
        private bool wallMode = true, endpointExists = false, endMode = false, startPointExists = false, startMode = false;

        public GameObject buttonPrefab, objectPrefab, endPrefab;
        public MyNodes Source;
        public bool ShowPath = false;
        public Stack<MyNodes> path;

        Camera Cm;
        MyNodes Destination;
        Graph graph = new Graph();
        List<int> notToUse = new List<int>();
        
        
        
        

        private void OnDisable()// recommended to do this in unity's tutorial. ccompletes threads instead of abandoning them
        {
            jobHandle.Complete();
        }

        void Start()  // Just references the camera and creates the nodes of the graph using the job system. 
        {
            Cm = GameObject.Find("Main Camera").GetComponent<Camera>();
            CreateGraphNodes();
        }

////////////////////////////////////////////////////////////   User Input Functions   //////////////////////////////////////////////////////////////////////////
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))  // these three modes let you choose what type of setting you wish to make for the grid
            {
                Debug.Log("Add Wall");
                wallMode = true;
                endMode = false;
                startMode = false;
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                Debug.Log("Set source node");
                wallMode = false;
                endMode = false;
                startMode = true;
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                Debug.Log("Set end node");
                wallMode = false;
                endMode = true;
                startMode = false;
            }

            if (Input.GetMouseButtonDown(0) && wallMode)
            {
                x = (Input.mousePosition.x - Input.mousePosition.x % 50) / 50;  //deciphers the coordinates on the grid where we click
                y = (Input.mousePosition.y - Input.mousePosition.y % 50) / 50;
                //Debug.Log(x + "    " + y);
                GameObject gO = Instantiate(buttonPrefab, GameObject.Find("Canvas").GetComponent<Transform>().Find("Buttons").GetComponent<Transform>()) as GameObject;// creates a button object on the grid to show a wall
                gO.transform.position = Cm.ViewportToScreenPoint(new Vector3((x / 16) + 1 / 32f, (y / 12) + 1 / 24f, 0));// places the object at the correct grid

                foreach (MyNodes node in graph.Nodelist)
                {
                    if (node.x == x && node.y == y)
                    {
                        if(!(notToUse.Contains(node.ID)))        // checks which node has these coordinates and adds its ID to a list.
                        notToUse.Add(node.ID);
                       //graph.Nodelist.Remove(node);                                                       // attempts to remove that node from the list
                       // node.setUsability(0);                                                             // attempts to set usabilty as non usable
                       //Debug.Log("Unusable Node: " + node.x + "," + node.y + "-usability(" + node.isUsable + ")");
                    }
                }
            }
            if (Input.GetMouseButtonDown(0) && endMode && !endpointExists)  // while endmode is on, alllows you to set the destination only once
            {
                x = (Input.mousePosition.x - Input.mousePosition.x % 50) / 50;  // this bit works the same as the wallmode except the prefab is different
                y = (Input.mousePosition.y - Input.mousePosition.y % 50) / 50;
                GameObject gO = Instantiate(endPrefab, GameObject.Find("Canvas").GetComponent<Transform>().Find("Buttons").GetComponent<Transform>()) as GameObject;
                gO.transform.position = Cm.ViewportToScreenPoint(new Vector3((x / 16) + 1 / 32f, (y / 12) + 1 / 24f, 0));
                endpointExists = true;

                foreach (MyNodes node in graph.Nodelist)
                {
                    if ((x == node.x) && (y == node.y))
                    {
                        Destination = node;                                                 // stores the reference to the node chosen to be the destination
                        Debug.Log("Destination Node Coordinates : " + Destination.x + "," + Destination.y);
                    }
                }
            }
            if (Input.GetMouseButtonDown(0) && startMode && !startPointExists)// allows the setting of initial point
            {
                x = (Input.mousePosition.x - Input.mousePosition.x % 50) / 50;  // this also works the same as the wall mode with different prefab
                y = (Input.mousePosition.y - Input.mousePosition.y % 50) / 50;
                GameObject gO = Instantiate(objectPrefab, GameObject.Find("Canvas").GetComponent<Transform>().Find("Buttons").GetComponent<Transform>()) as GameObject;
                gO.transform.position = Cm.ViewportToScreenPoint(new Vector3((x / 16) + 1 / 32f, (y / 12) + 1 / 24f, 0));
                startPointExists = true;

                foreach (MyNodes node in graph.Nodelist)
                {
                    if ((x == node.x) && (y == node.y))
                    {
                        Source = node;                                         // stores the reference to the node chosen to be the source.
                        Debug.Log("Start Node Coordinates : " + Source.x + "," + Source.y);
                    }

                }
            }
            if (Input.GetKeyDown(KeyCode.Space))  // initiates the creation of edges and applies the path finding algorithm function after that
            {
                //foreach (MyNodes node in graph.Nodelist)
                //{
                //    Debug.Log("ID: " + node.ID + ", Coordinates: " +node.x + "," + node.y + " , Usability: " + node.isUsable);
                //}
                //foreach(int i in notToUse)
                //{
                //    Debug.Log(i);
                //}
                CreateEdges();
                BellmanFordAlgo(graph, Source);
            }
        }
/////////////////////////////////////////////////////////// Node generation function with the help of job system//////////////////////////////////////////////
        //[Unity.Burst.BurstCompile]
        public struct ImplementAlgo : IJobParallelFor
        {
            public static int ID;
            public int I;
            public NativeArray<MyNodes> NodeList;
            public void Execute(int index)
            {
                ID = I * 12 + index;
                NodeList[index] = new MyNodes(I, index, ID);
            }
        }
        ImplementAlgo doJob;
        JobHandle jobHandle;
        private void CreateGraphNodes()
        {
            for (int i = 0; i < 16; i++)
            {
                NativeArray<MyNodes> nL = new NativeArray<MyNodes>(12, Allocator.TempJob);

                doJob = new ImplementAlgo()
                {
                    I = i,
                    NodeList = nL
                };
                jobHandle = doJob.Schedule(nL.Length, 1);
                jobHandle.Complete();

                for (int j = 0; j < 12; j++)
                {
                    graph.Nodelist.Add(nL[j]);
                }
                nL.Dispose();
            }
        }

        //foreach (MyNodes node in graph.Nodelist)
        //{
        //    Debug.Log(node.x + "," + node.y + ":"+ node.isUsable);
        //}

////////////////////////////////////////////////////////////   Edge Generation     /////////////////////////////////////////////////////////////////////////////
        private void CreateEdges()
        {
            for (int i = 0; i < graph.Nodelist.Count; i++)  ///// iterates through every vertice.
            {
                if (!(notToUse.Contains( graph.Nodelist[i].ID))) /// checks if the vertice is usable
                {
                    for (int j = 0; (j != i) && (j < graph.Nodelist.Count); j++) ///iterates through every vertice which isnt the first vertice
                    {
                        if (!(notToUse.Contains(graph.Nodelist[j].ID)))/// checks if the vertice is usable
                        {
                            bool edgeExists = false;
                            foreach (MyEdges edge in graph.Edgelist)/// Checks if there already are one or more edges between the two vertices
                            {
                                if ((edge.A.ID == graph.Nodelist[i].ID || edge.A.ID == graph.Nodelist[j].ID) && (edge.B.ID == graph.Nodelist[j].ID || edge.B.ID == graph.Nodelist[i].ID))
                                {
                                    edgeExists = true;
                                    //Debug.Log("GRAPH EXISTS!!");
                                }
                            }

                            if (!edgeExists) // if not then creates two directed edges between the two vertices 
                            {
                                if ((Mathf.Abs(graph.Nodelist[i].x - graph.Nodelist[j].x) < 2) && (Mathf.Abs(graph.Nodelist[i].y - graph.Nodelist[j].y) < 2))// ensures that edges form only between adjacent vertices
                                {
                                    if ((Mathf.Abs(graph.Nodelist[i].x - graph.Nodelist[j].x) + Mathf.Abs(graph.Nodelist[i].y - graph.Nodelist[j].y)) == 2)
                                    {
                                        graph.Edgelist.Add(new MyEdges(graph.Nodelist[i], graph.Nodelist[j], 1.5f));
                                        graph.Edgelist.Add(new MyEdges(graph.Nodelist[j], graph.Nodelist[i], 1.5f));// creates edges of a higher weigth if the vertices are diagonally adjacent
                                    }
                                    else
                                    {
                                        graph.Edgelist.Add(new MyEdges(graph.Nodelist[i], graph.Nodelist[j], 1f));
                                        graph.Edgelist.Add(new MyEdges(graph.Nodelist[j], graph.Nodelist[i], 1f));// creates edges of a lower weigth if the vertices are directly adjacent
                                    }
                                }
                            }
                        }
                    }
                }
            }
        

            //foreach(MyEdges edge in graph.Edgelist)
            //{
            //    Debug.Log("Start:" + edge.A.x + "," + edge.A.y + "   End: " + edge.B.x + "," + edge.B.y); //lists all edges in the list
            //}
        }
 /////////////////////////////////////////////////////////////////// Algorithm Implementation Function /////////////////////////////////////////////////////////////////

        private void BellmanFordAlgo(Graph graph, MyNodes sourceNode) // takes in a graph reference and a source node reference as argument
        {
            Dictionary<MyNodes, NodeValue> Distance = new Dictionary<MyNodes, NodeValue>(); // creates a dictionary of nodes as keys and nodevalues as values

            for (int i = 0; i < graph.Nodelist.Count; i++)
            {
                NodeValue nodeValue = new NodeValue(graph.Nodelist[i], float.MaxValue);// creates node value with the cost of all nodes to max value and all prev nodes for that node as the node itself
                Distance.Add(graph.Nodelist[i], nodeValue); // adds the node value to the dictionary.
            }

            Distance[sourceNode].Cost = 0; // changes the cost of the source node to 0;

            for (int i = 0; i < graph.Nodelist.Count; i++) // for each vertice.
            {
                for (int j = 0; j < graph.Edgelist.Count; j++)// traverses each edge in the list
                {
                    MyNodes startNode = graph.Edgelist[j].A;
                    MyNodes endtNode = graph.Edgelist[j].B;
                    float w = graph.Edgelist[j].weight;

                    if (Distance[startNode].Cost != float.MaxValue && ((Distance[startNode].Cost + w) < Distance[endtNode].Cost)) // checks if the edge originates from a source node and if the cost of destination node is higher than the cost of movement from source node
                    {
                        Distance[endtNode].Cost = Distance[startNode].Cost + w; // updates the cost if its higher than the traversal cost 
                        Distance[endtNode].prevNode = startNode;// updates the previous node of the destination node as the source node of the edge.
                    }
                }
            }
            //foreach (KeyValuePair<MyNodes, NodeValue> pair in Distance)
            //{
            //    Debug.Log("Coordinates: " + pair.Key.x + "," + pair.Key.y + "Cost: " + pair.Value.Cost);
            //}
            path = new Stack<MyNodes>(); // creates a stack for the path.
            path.Push(Destination);  // pushes the destination node as the first input.

            while (Distance[path.Peek()].prevNode.ID != sourceNode.ID)// while the previous node is not the source node of the 1st node in stack
            {
                path.Push(Distance[path.Peek()].prevNode); // pushes in the previous node.
            }
            ShowPath = true; // gives the green light to start showing the path.
        }
        
    }
}
