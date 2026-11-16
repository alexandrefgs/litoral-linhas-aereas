using Litoral.Domain.Common;

namespace Litoral.Domain.Planning;

public sealed record RosterEdge(Guid RouteId, string From, string To);

public static class RosterPlanner
{
    public const int MinSequence = 2;
    public const int MaxSequence = 7;
    private const int SearchLimit = 20_000;

    public static IReadOnlyList<int> SplitSequences(int total)
    {
        if (total < MinSequence) total = MinSequence;

        var count = (int)Math.Ceiling(total / (double)MaxSequence);
        var size = total / count;
        var extra = total % count;

        return Enumerable.Range(0, count).Select(i => size + (i < extra ? 1 : 0)).ToList();
    }

    public static IReadOnlyList<IReadOnlyList<RosterEdge>> Plan(string baseIcao, IReadOnlyList<RosterEdge> edges, int totalFlights, Random random)
    {
        var outgoing = edges.GroupBy(e => e.From).ToDictionary(g => g.Key, g => g.ToList());
        var distance = DistanceToBase(baseIcao, edges);

        var sequences = new List<IReadOnlyList<RosterEdge>>();
        var remaining = Math.Max(totalFlights, MinSequence);

        foreach (var target in SplitSequences(remaining))
        {
            if (remaining < MinSequence) break;

            var sequence = Candidates(target, remaining, allowSingleLeftover: false)
                .Select(length => FindLoop(baseIcao, length, outgoing, distance, random))
                .FirstOrDefault(found => found is not null);

            if (sequence is null) continue;

            sequences.Add(sequence);
            remaining -= sequence.Count;
        }

        while (remaining >= MinSequence)
        {
            var sequence = Candidates(Math.Min(remaining, MaxSequence), remaining, allowSingleLeftover: true)
                .Select(length => FindLoop(baseIcao, length, outgoing, distance, random))
                .FirstOrDefault(found => found is not null);

            if (sequence is null) break;

            sequences.Add(sequence);
            remaining -= sequence.Count;
        }

        if (sequences.Count == 0)
            throw new DomainException($"There are not enough routes from {baseIcao} to build a roster.");

        return sequences;
    }

    private static IEnumerable<int> Candidates(int target, int remaining, bool allowSingleLeftover)
    {
        var max = Math.Min(MaxSequence + 1, remaining);

        for (var delta = 0; delta <= MaxSequence; delta++)
        {
            foreach (var length in delta == 0 ? [target] : new[] { target - delta, target + delta })
            {
                if (length < MinSequence || length > max) continue;
                if (!allowSingleLeftover && remaining - length == 1) continue;
                yield return length;
            }
        }
    }

    private static Dictionary<string, int> DistanceToBase(string baseIcao, IReadOnlyList<RosterEdge> edges)
    {
        var incoming = edges.GroupBy(e => e.To).ToDictionary(g => g.Key, g => g.Select(e => e.From).ToList());
        var distance = new Dictionary<string, int> { [baseIcao] = 0 };
        var queue = new Queue<string>([baseIcao]);

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            if (!incoming.TryGetValue(node, out var sources)) continue;

            foreach (var source in sources)
            {
                if (distance.ContainsKey(source)) continue;
                distance[source] = distance[node] + 1;
                queue.Enqueue(source);
            }
        }

        return distance;
    }

    private static List<RosterEdge>? FindLoop(
        string baseIcao,
        int length,
        Dictionary<string, List<RosterEdge>> outgoing,
        Dictionary<string, int> distance,
        Random random)
    {
        var path = new List<RosterEdge>(length);
        var steps = 0;

        bool Walk(string node)
        {
            if (++steps > SearchLimit) return false;

            var left = length - path.Count;
            if (left == 0) return node == baseIcao;
            if (!outgoing.TryGetValue(node, out var options)) return false;

            foreach (var edge in options.OrderBy(_ => random.Next()))
            {
                var arrivesAtBase = edge.To == baseIcao;
                if (arrivesAtBase != (left == 1)) continue;
                if (!arrivesAtBase && (!distance.TryGetValue(edge.To, out var back) || back > left - 1)) continue;

                path.Add(edge);
                if (Walk(edge.To)) return true;
                path.RemoveAt(path.Count - 1);
            }

            return false;
        }

        return Walk(baseIcao) ? path : null;
    }
}