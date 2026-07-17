using UnityEngine;

namespace DeepEarth.Map
{
    /// <summary>
    /// Post-processes room type assignments to enforce all four map rules.
    /// Rules are NEVER applied during grid or path generation — only here.
    ///
    /// Rule1 — Floors 1-10 (1-indexed): only Mine / Event / Merchant allowed.
    /// Rule2 — Elite / Rest / Merchant cannot appear consecutively on a direct edge.
    /// Rule3 — Siblings (children of the same parent node) must have distinct room types.
    /// Rule4 — Floor 49 (1-indexed, 0-indexed=48) cannot be Rest.
    ///
    /// Fixed floors are exempt from reassignment.
    /// Violations are fixed by random reassignment of the offending node only.
    /// Iteration continues until no violations remain or the maximum count is reached.
    /// </summary>
    public class RuleValidator
    {
        private const int MaxIterations       = 200;
        private const int MaxReassignAttempts = 50;

        private const int EarlyFloorMax  = 10; // 0-indexed: floors 0-9 (= 1-indexed floors 1-10)
        private const int Rule4FloorIdx  = 48; // 0-indexed: floor 49 (1-indexed)

        private static readonly RoomType[] EarlyFloorAllowed = { RoomType.Mine, RoomType.Event, RoomType.Merchant };

        private readonly RoomGenerationConfig _config;
        private readonly IRandomProvider      _rng;

        private int _treasureFloorIdx;
        private int _restFloorIdx;

        public RuleValidator(RoomGenerationConfig config, IRandomProvider rng)
        {
            _config = config;
            _rng    = rng;
        }

        /// <summary>Validates and repairs all rule violations in <paramref name="mapData"/>.</summary>
        public void Validate(MapData mapData)
        {
            _treasureFloorIdx = _config.TreasureFloorIndex;
            _restFloorIdx     = _config.RestFloorIndex;

            for (int iter = 0; iter < MaxIterations; iter++)
            {
                bool anyViolation = false;

                for (int floor = 0; floor < mapData.Floors; floor++)
                {
                    for (int col = 0; col < mapData.Columns; col++)
                    {
                        MapNode node = mapData.Grid[floor, col];
                        if (!node.IsActive || IsExempt(node)) continue;

                        if (FixViolations(mapData, node))
                            anyViolation = true;
                    }
                }

                if (!anyViolation)
                {
                    Debug.Log($"[Map]\nRuleValidator: all rules satisfied after {iter + 1} pass(es)");
                    return;
                }
            }

            Debug.LogWarning($"[Map]\nRuleValidator: reached max iterations ({MaxIterations}) — residual violations may remain");
        }

        // ─── Exemption ─────────────────────────────────────────────────────────

        private bool IsExempt(MapNode node)
            => node.Floor == _treasureFloorIdx || node.Floor == _restFloorIdx;

        // ─── Violation detection + repair ──────────────────────────────────────

        private bool FixViolations(MapData mapData, MapNode node)
        {
            bool violated = false;

            if (BreaksRule1(node))
            {
                AssignEarlyFloorType(node);
                violated = true;
            }

            if (BreaksRule2(mapData, node))
            {
                ReassignExcluding(node, node.RoomType);
                violated = true;
            }

            if (BreaksRule3(mapData, node))
            {
                ReassignExcluding(node, node.RoomType);
                violated = true;
            }

            if (BreaksRule4(node))
            {
                ReassignExcluding(node, RoomType.Rest);
                violated = true;
            }

            // Rule Mine: Mine cannot have more than 1 outgoing connection.
            // Checked last so that prior rule fixes (which may assign Mine) are caught
            // in the same pass rather than waiting for the next iteration.
            if (BreaksRuleMine(node))
            {
                ReassignNonMine(node);
                violated = true;
            }

            return violated;
        }

        // ─── Rule 1 ────────────────────────────────────────────────────────────

        /// <summary>Floors 1-10 (0-indexed 0-9): only Mine/Event/Merchant.</summary>
        private static bool BreaksRule1(MapNode node)
        {
            if (node.Floor >= EarlyFloorMax) return false;
            return node.RoomType != RoomType.Mine
                && node.RoomType != RoomType.Event
                && node.RoomType != RoomType.Merchant;
        }

        private void AssignEarlyFloorType(MapNode node)
        {
            node.RoomType = EarlyFloorAllowed[_rng.Range(0, EarlyFloorAllowed.Length)];
        }

        // ─── Rule 2 ────────────────────────────────────────────────────────────

        /// <summary>Elite / Rest / Merchant may not follow the same type on a direct edge.</summary>
        private static bool BreaksRule2(MapData mapData, MapNode node)
        {
            if (!IsConsecutiveRestricted(node.RoomType)) return false;

            foreach (var conn in node.IncomingConnections)
            {
                MapNode parent = mapData.Grid[conn.FromFloor, conn.FromColumn];
                if (parent.IsActive && parent.RoomType == node.RoomType)
                    return true;
            }
            return false;
        }

        private static bool IsConsecutiveRestricted(RoomType type)
            => type == RoomType.Elite || type == RoomType.Rest || type == RoomType.Merchant;

        // ─── Rule 3 ────────────────────────────────────────────────────────────

        /// <summary>Siblings (same parent's children) must all have distinct room types.</summary>
        private static bool BreaksRule3(MapData mapData, MapNode node)
        {
            foreach (var conn in node.IncomingConnections)
            {
                MapNode parent = mapData.Grid[conn.FromFloor, conn.FromColumn];
                if (!parent.IsActive) continue;

                foreach (var sibConn in parent.OutgoingConnections)
                {
                    // Skip self
                    if (sibConn.ToFloor == node.Floor && sibConn.ToColumn == node.Column) continue;

                    MapNode sibling = mapData.Grid[sibConn.ToFloor, sibConn.ToColumn];
                    if (sibling.IsActive && sibling.RoomType == node.RoomType)
                        return true;
                }
            }
            return false;
        }

        // ─── Rule 4 ────────────────────────────────────────────────────────────

        /// <summary>Floor 49 (1-indexed) = floor 48 (0-indexed) cannot be Rest.</summary>
        private static bool BreaksRule4(MapNode node)
            => node.Floor == Rule4FloorIdx && node.RoomType == RoomType.Rest;

        // ─── Rule Mine ─────────────────────────────────────────────────────────

        /// <summary>Mine Room must have OutDegree == 1 (no branching).</summary>
        private static bool BreaksRuleMine(MapNode node)
            => node.RoomType == RoomType.Mine && node.OutgoingConnections.Count > 1;

        /// <summary>
        /// Reassigns a Mine-violating node to a non-Mine type.
        /// For early-floor nodes, only Event or Merchant are valid alternatives
        /// (the third early-floor type, Mine, is what we're avoiding).
        /// </summary>
        private void ReassignNonMine(MapNode node)
        {
            // Early floors: only Event / Merchant are valid non-Mine alternatives
            if (node.Floor < EarlyFloorMax)
            {
                node.RoomType = _rng.Range(0, 2) == 0 ? RoomType.Event : RoomType.Merchant;
                return;
            }

            for (int attempt = 0; attempt < MaxReassignAttempts; attempt++)
            {
                RoomType picked = _config.PickRoomType(_rng);
                if (picked != RoomType.Mine)
                {
                    node.RoomType = picked;
                    return;
                }
            }
            // Guaranteed non-Mine, non-consecutive-restricted fallback
            node.RoomType = RoomType.Monster;
        }

        // ─── Reassignment ──────────────────────────────────────────────────────

        private void ReassignExcluding(MapNode node, RoomType forbidden)
        {
            for (int attempt = 0; attempt < MaxReassignAttempts; attempt++)
            {
                RoomType picked = _config.PickRoomType(_rng);
                if (picked != forbidden)
                {
                    node.RoomType = picked;
                    return;
                }
            }
            // Safe fallback: Mine has no consecutive or sibling restrictions.
            node.RoomType = RoomType.Mine;
        }
    }
}
