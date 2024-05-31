// See https://aka.ms/new-console-template for more information

using QuikGraph;
using QuikGraph.Algorithms;
using QuikGraph.Algorithms.Observers;
using QuikGraph.Algorithms.RankedShortestPath;
using QuikGraph.Algorithms.ShortestPath;

Console.WriteLine("Hello, World!");

BidirectionalGraph<int, TaggedEdge<int, string>> graph = new();

graph.AddVertex(1);
graph.AddVertex(2);
graph.AddVertex(3);
graph.AddVertex(4);

graph.AddVertex(5);
graph.AddVertex(6);
graph.AddVertex(7);
graph.AddVertex(8);
graph.AddVertex(9);
graph.AddVertex(10);

graph.AddEdge(new TaggedEdge<int, string>(5, 6, "5 -> 6"));
graph.AddEdge(new TaggedEdge<int, string>(5, 7, "5 -> 7"));
graph.AddEdge(new TaggedEdge<int, string>(6, 8, "6 -> 8"));
graph.AddEdge(new TaggedEdge<int, string>(6, 10, "7 -> 8"));
graph.AddEdge(new TaggedEdge<int, string>(7, 8, "7 -> 8"));
graph.AddEdge(new TaggedEdge<int, string>(8, 9, "8 -> 9"));
graph.AddEdge(new TaggedEdge<int, string>(9, 10, "9 -> 10"));
graph.AddEdge(new TaggedEdge<int, string>(10, 5, "10 -> 5"));

graph.AddEdge(new TaggedEdge<int, string>(1, 2, "1 -> 2"));
graph.AddEdge(new TaggedEdge<int, string>(1, 3, "1 -> 3"));
graph.AddEdge(new TaggedEdge<int, string>(2, 4, "2 -> 4"));



var dijstra = new DijkstraShortestPathAlgorithm<int, TaggedEdge<int, string>>(graph, e => 1);


var dijstraObserver = new VertexPredecessorRecorderObserver<int, TaggedEdge<int, string>>();
using (dijstraObserver.Attach(dijstra))
{
    dijstra.Compute(6);
    // get all paths

}



foreach (var vertex in dijstra.GetDistances())
{
    Console.WriteLine($"Vertex {vertex.Key} is at distance {vertex.Value}");
    // get all paths from vertex to root 
    var all = graph.RankedShortestPathHoffmanPavley(edge => 1, 6, vertex.Key, 99);

    foreach (var path in all)
    {
        Console.WriteLine($"Path: {string.Join(" -> ", path)}");
        // print length of path using edge weights
    }
}