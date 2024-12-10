using System;
using System.Collections.Generic;
using System.Linq;

namespace Hercules.Analysis
{
    public class GraphNode<T> : IEquatable<GraphNode<T>> where T : class
    {
        public T Value { get; }
        public string Id { get; }
        public HashSet<GraphNode<T>> References { get; } = new();
        public HashSet<GraphNode<T>> ReferencedBy { get; } = new();
        public GraphNode(string id, T value)
        {
            Id = id;
            Value = value;
        }

        public bool Equals(GraphNode<T>? other) => other is not null && other.Id == Id;

        public override bool Equals(object? obj) => obj is GraphNode<T> other && other.Id == Id;

        public override int GetHashCode() => Id.GetHashCode();
    }

    public class GraphModel<T> where T : class
    {
        public IReadOnlyDictionary<string, GraphNode<T>> Nodes => nodes;
        private readonly Dictionary<string, GraphNode<T>> nodes = new Dictionary<string, GraphNode<T>>();
        private readonly Func<T, string> idFun;

        public GraphModel(Func<T, string> idFun)
        {
            this.idFun = idFun;
        }

        public void AddNodes(IReadOnlyList<T> values)
        {
            foreach (var value in values)
            {
                AddNode(value);
            }
        }

        public GraphNode<T> AddNode(T value)
        {
            var id = idFun(value);
            return nodes.GetOrAdd(id, () => new GraphNode<T>(id, value));
        }

        public void AddReference(GraphNode<T> fromNode, GraphNode<T> toNode)
        {
            fromNode.References.Add(toNode);
            toNode.ReferencedBy.Add(fromNode);
        }

        public IEnumerable<(GraphNode<T> from, GraphNode<T> to)> TraverseReferences(IEnumerable<GraphNode<T>> rootNodes, int maxDepth)
        {
            return TraverseEdges(rootNodes, true, maxDepth);
        }

        public IEnumerable<(GraphNode<T> from, GraphNode<T> to)> TraverseReferencedBy(IEnumerable<GraphNode<T>> rootNodes, int maxDepth)
        {
            return TraverseEdges(rootNodes, false, maxDepth);
        }

        public IEnumerable<(GraphNode<T> from, GraphNode<T> to)> TraverseEdges(IEnumerable<GraphNode<T>> rootNodes, bool direction, int maxDepth)
        {
            if (maxDepth <= 0)
                yield break;
            var processedNodes = new HashSet<GraphNode<T>>();
            var nodesToProcess = new HashSet<GraphNode<T>>(rootNodes);
            for (int depth = 0; depth < maxDepth; depth++)
            {
                var nodesList = nodesToProcess.ToList();
                nodesToProcess.Clear();
                foreach (var node in nodesList)
                {
                    if (processedNodes.Contains(node))
                        continue;
                    foreach (var reference in direction ? node.References : node.ReferencedBy)
                    {
                        if (direction)
                            yield return (node, reference);
                        else
                            yield return (reference, node);
                        nodesToProcess.Add(reference);
                    }
                    processedNodes.Add(node);
                }
            }
        }
    }
}
