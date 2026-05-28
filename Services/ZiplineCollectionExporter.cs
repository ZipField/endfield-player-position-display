using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using endfield_player_position_display.Models;

namespace endfield_player_position_display.Services
{
    public static class ZiplineCollectionExporter
    {
        public static string ExportMarksJson(IReadOnlyList<DetectedZiplineStop> stops)
        {
            List<Node> nodes = BuildNodes(stops);
            return "[" + string.Join(",", nodes.Select(FormatNode)) + "]";
        }

        public static string ExportRoutesJson(IReadOnlyList<DetectedZiplineStop> stops)
        {
            List<Node> nodes = BuildNodes(stops);
            List<List<Node>> groups = BuildConnectedGroups(nodes);
            return "[" + string.Join(",", groups.Select(FormatRoute)) + "]";
        }

        private static List<Node> BuildNodes(IReadOnlyList<DetectedZiplineStop> stops)
        {
            var nodes = new List<Node>();
            var byId = new Dictionary<string, Node>(StringComparer.Ordinal);
            Node previous = null;
            foreach (DetectedZiplineStop stop in stops ?? new DetectedZiplineStop[0])
            {
                string id = CreateId(stop);
                Node node;
                if (!byId.TryGetValue(id, out node))
                {
                    node = new Node(id, stop);
                    byId[id] = node;
                    nodes.Add(node);
                }

                if (previous != null && stop.ConnectToPrevious && !string.Equals(previous.Id, node.Id, StringComparison.Ordinal))
                {
                    previous.Connect.Add(node.Id);
                    node.Connect.Add(previous.Id);
                }

                previous = node;
            }

            return nodes;
        }

        private static List<List<Node>> BuildConnectedGroups(List<Node> nodes)
        {
            var result = new List<List<Node>>();
            var visited = new HashSet<string>(StringComparer.Ordinal);
            Dictionary<string, Node> byId = nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
            foreach (Node node in nodes)
            {
                if (visited.Contains(node.Id))
                {
                    continue;
                }

                var group = new List<Node>();
                var stack = new Stack<Node>();
                stack.Push(node);
                visited.Add(node.Id);
                while (stack.Count > 0)
                {
                    Node current = stack.Pop();
                    group.Add(current);
                    foreach (string id in current.Connect)
                    {
                        Node next;
                        if (byId.TryGetValue(id, out next) && visited.Add(id))
                        {
                            stack.Push(next);
                        }
                    }
                }

                group.Sort((a, b) => a.FirstOrder.CompareTo(b.FirstOrder));
                result.Add(group);
            }

            return result;
        }

        private static string FormatNode(Node node)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{{\"id\":\"{0}\",\"name\":\"未命名滑索\",\"connect\":[{1}],\"h\":{2},\"direction\":\"{3}\"}}",
                node.Id,
                string.Join(",", node.Connect.OrderBy(id => id, StringComparer.Ordinal).Select(id => "\"" + id + "\"")),
                FormatHeight(node.Stop.Y),
                node.Stop.Direction);
        }

        private static string FormatRoute(List<Node> nodes)
        {
            return "{\"name\":\"未命名路线\",\"marks\":[" + string.Join(",", nodes.Select(node => "\"" + node.Id + "\"")) + "]}";
        }

        private static string CreateId(DetectedZiplineStop stop)
        {
            return string.Format(CultureInfo.InvariantCulture, "({0},{1})", stop.X, stop.Z);
        }

        private static string FormatHeight(double value)
        {
            double rounded = Math.Round(value);
            if (Math.Abs(value - rounded) < 0.000001)
            {
                return rounded.ToString("0", CultureInfo.InvariantCulture);
            }

            return value.ToString("0.########", CultureInfo.InvariantCulture);
        }

        private sealed class Node
        {
            public Node(string id, DetectedZiplineStop stop)
            {
                Id = id;
                Stop = stop;
                FirstOrder = stop.Order;
                Connect = new HashSet<string>(StringComparer.Ordinal);
            }

            public string Id { get; }
            public DetectedZiplineStop Stop { get; }
            public int FirstOrder { get; }
            public HashSet<string> Connect { get; }
        }
    }
}
