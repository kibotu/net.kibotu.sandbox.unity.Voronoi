using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Source;
using UnityEngine;
using UnityEngine.UI;
using Voronoi;
using Point = Voronoi.Point;

namespace Assets.Voronoi.Demos
{
    public class VoronoiDemo : MonoBehaviour
    {
        public int numSites = 36;
        public bool isAnimated = false;
        public Bounds bounds;

        private List<Point> sites;
        private FortuneVoronoi voronoi;
        private VoronoiGraph graph;
        public InputField AmountOfSites;

        public void Animate(bool isAnimated)
        {
            this.isAnimated = isAnimated;
        }

        void Start()
        {
            AmountOfSites.onSubmit.AddListener(value => numSites = int.Parse(value));
            AmountOfSites.text.text = "" + numSites;

            sites = new List<Point>();
            voronoi = new FortuneVoronoi();

            CreateSites(true, true, numSites);

            CreateLines();
        }

        void FixedUpdate()
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                Reset();
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                Relax();
            }
            if (isAnimated)
            {
                Relax();
                RedrawAsync();
            }
        }

        public void Reset()
        {
            CreateSites(true, false); 
            RedrawAsync();
        }

        public void Relax()
        {
            RelaxSites(1);
            RedrawAsync();
        }

        public void RedrawAsync()
        {
            StopCoroutine("RedrawLines");
            StartCoroutine("RedrawLines");
        }

        public IEnumerator RedrawLines()
        {
            yield return new WaitForEndOfFrame();
            lines.Reset();
            CreateLines();
            yield return null;
        }

        void Compute(List<Point> sites)
        {
            this.sites = sites;
            this.graph = this.voronoi.Compute(sites, this.bounds);
        }

        void CreateSites(bool clear = true, bool relax = false, int relaxCount = 2)
        {
            List<Point> sites = new List<Point>();
            if (!clear)
            {
                sites = this.sites.Take(this.sites.Count).ToList();
            }

            // create vertices
            for (int i = 0; i < numSites; i++)
            {
                Point site = new Point(Random.Range(bounds.min.x, bounds.max.x), Random.Range(bounds.min.z, bounds.max.z), 0);
                sites.Add(site);
            }

            Compute(sites);

            if (relax)
            {
                RelaxSites(relaxCount);
            }
        }

        void RelaxSites(int iterations)
        {
            for (int i = 0; i < iterations; i++)
            {
                if (!this.graph)
                {
                    return;
                }

                Point site;
                List<Point> sites = new List<Point>();
                float dist = 0;

                float p = 1f / graph.cells.Count * 0.1f;

                for (int iCell = graph.cells.Count - 1; iCell >= 0; iCell--)
                {
                    global::Voronoi.Cell cell = graph.cells[iCell];
                    float rn = Random.value;

                    // probability of apoptosis
                    if (rn < p)
                    {
                        continue;
                    }

                    site = CellCentroid(cell);
                    dist = Distance(site, cell.site);

                    // don't relax too fast
                    if (dist > 2)
                    {
                        site.x = (site.x + cell.site.x) / 2;
                        site.y = (site.y + cell.site.y) / 2;
                    }
                    // probability of mytosis
                    if (rn > (1 - p))
                    {
                        dist /= 2;
                        sites.Add(new Point(site.x + (site.x - cell.site.x) / dist, site.y + (site.y - cell.site.y) / dist));
                    }
                    sites.Add(site);
                }

                Compute(sites);
            }
        }

        float Distance(Point a, Point b)
        {
            float dx = a.x - b.x;
            float dy = a.y - b.y;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        Point CellCentroid(global::Voronoi.Cell cell)
        {
            float x = 0f;
            float y = 0f;
            Point p1, p2;
            float v;

            for (int iHalfEdge = cell.halfEdges.Count - 1; iHalfEdge >= 0; iHalfEdge--)
            {
                HalfEdge halfEdge = cell.halfEdges[iHalfEdge];
                p1 = halfEdge.GetStartPoint();
                p2 = halfEdge.GetEndPoint();
                v = p1.x * p2.y - p2.x * p1.y;
                x += (p1.x + p2.x) * v;
                y += (p1.y + p2.y) * v;
            }
            v = CellArea(cell) * 6;
            return new Point(x / v, y / v);
        }

        float CellArea(global::Voronoi.Cell cell)
        {
            float area = 0.0f;
            Point p1, p2;

            for (int iHalfEdge = cell.halfEdges.Count - 1; iHalfEdge >= 0; iHalfEdge--)
            {
                HalfEdge halfEdge = cell.halfEdges[iHalfEdge];
                p1 = halfEdge.GetStartPoint();
                p2 = halfEdge.GetEndPoint();
                area += p1.x * p2.y;
                area -= p1.y * p2.x;
            }
            area /= 2;
            return area;
        }

        void OnDrawGizmos()
        {
            if (graph)
            {
                foreach (global::Voronoi.Cell cell in graph.cells)
                {
                    Gizmos.color = Color.black;
                    Gizmos.DrawCube(new Vector3(cell.site.x, 0, cell.site.y), Vector3.one);

                    if (cell.halfEdges.Count > 0)
                    {
                        foreach (HalfEdge halfEdge in cell.halfEdges)
                        {
                            Edge edge = halfEdge.edge;

                            if (edge.va && edge.vb)
                            {
                                Gizmos.color = Color.red;
                                Gizmos.DrawLine(new Vector3(edge.va.x, 0, edge.va.y),
                                    new Vector3(edge.vb.x, 0, edge.vb.y));
                            }
                        }
                    }
                }
            }
        }

        private LinesRenderer lines;

        void CreateLines()
        {
            if (lines == null)
                lines = GetComponent<LinesRenderer>();

            if (graph)
            {
                foreach (global::Voronoi.Cell cell in graph.cells)
                {
//                Gizmos.color = Color.black;
//                Gizmos.DrawCube(new Vector3(cell.site.x, 0, cell.site.y), Vector3.one);

                    if (cell.halfEdges.Count > 0)
                    {
                        foreach (HalfEdge halfEdge in cell.halfEdges)
                        {
                            Edge edge = halfEdge.edge;

                            if (edge.va && edge.vb)
                            {
                                lines.AddLine(new Vector2(edge.va.x, edge.va.y), new Vector2(edge.vb.x,edge.vb.y));
                            }
                        }
                    }
                }
            }
        }
    }
}