using System.Reflection.Emit;

namespace Flee.InternalTypes
{
    [Obsolete("Manages branch information and allows us to determine if we should emit a short or long branch")]
    internal class BranchManager
    {
        private readonly IList<BranchInfo> _branchInfos;

        public BranchManager()
        {
            _branchInfos = new List<BranchInfo>();
        }

        /// <summary>
        /// check if any long branches exist
        /// </summary>
        /// <returns></returns>
        public bool HasLongBranches()
        {
            foreach (BranchInfo bi in _branchInfos)
            {
                if (bi.ComputeIsLongBranch()) return true;
            }
            return false;
        }

        /// <summary>
        /// Determine whether to use short or long branches.
        /// This advances the ilg offset with No-op to adjust
        /// for the long branches needed.
        /// </summary>
        /// <remarks></remarks>
        public bool ComputeBranches()
        {
            //
            // we need to iterate in reverse order of the
            // starting location, as branch between our 
            // branch could push our branch to a long branch.
            //
            for( var idx=_branchInfos.Count-1; idx >= 0; idx--)
            {
                var bi = _branchInfos[idx];

                // count long branches between
                int longBranchesBetween = 0;
                for( var ii=idx+1; ii < _branchInfos.Count; ii++)
                {
                    var bi2 = _branchInfos[ii];
                    if (bi2.IsBetween(bi) && bi2.ComputeIsLongBranch())
                        ++longBranchesBetween;
                }

                // Adjust the branch as necessary
                bi.AdjustForLongBranchesBetween(longBranchesBetween);
            }

            int longBranchCount = 0;

            // Adjust the start location of each branch
            foreach (BranchInfo bi in _branchInfos)
            {
                // Save the short/long branch type
                bi.BakeIsLongBranch();

                // Adjust the start location as necessary
                bi.AdjustForLongBranches(longBranchCount);

                // Keep a tally of the number of long branches
                longBranchCount += Convert.ToInt32(bi.IsLongBranch);
            }

            return  (longBranchCount > 0);
        }


        /// <summary>
        /// Determine if a branch from a point to a label will be long
        /// </summary>
        /// <param name="ilg"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public bool IsLongBranch(FleeILGenerator ilg)
        {
            ILLocation startLoc = new ILLocation(ilg.Length);

            foreach (var bi in _branchInfos)
            {
                if (bi.Equals(startLoc))
                    return bi.IsLongBranch;
            }

            // we don't really know since this branch didn't exist.
            // we could throw an exceptio but 
            // do a long branch to be safe.
            return true;
        }

        /// <summary>
        /// Add a branch from a location to a target label
        /// </summary>
        /// <param name="ilg"></param>
        /// <param name="target"></param>
        /// <remarks></remarks>
        public void AddBranch(FleeILGenerator ilg, Label target)
        {
            ILLocation startLoc = new ILLocation(ilg.Length);

            BranchInfo bi = new BranchInfo(startLoc, target);
            // branches will be sorted in order
            _branchInfos.Add(bi);
        }


        /// <summary>
        /// Set the position for a label
        /// </summary>
        /// <param name="ilg"></param>
        /// <param name="target"></param>
        /// <remarks></remarks>
        public void MarkLabel(FleeILGenerator ilg, Label target)
        {
            int pos = ilg.Length;

            foreach (BranchInfo bi in _branchInfos)
            {
                bi.Mark(target, pos);
            }
        }

        public override string ToString()
        {
            string[] arr = new string[_branchInfos.Count];

            for (int i = 0; i <= _branchInfos.Count - 1; i++)
            {
                arr[i] = _branchInfos[i].ToString();
            }

            return string.Join(System.Environment.NewLine, arr);
        }
    }

    [Obsolete("Represents a location in an IL stream")]
    internal class ILLocation : IEquatable<ILLocation>, IComparable<ILLocation>
    {
        private int _position;

        /// <summary>
        /// ' Long branch is 5 bytes; short branch is 2; so we adjust by the difference
        /// </summary>
        private const int LongBranchAdjust = 3;

        /// <summary>
        /// Length of the Br_s opcode
        /// </summary>
        private const int BrSLength = 2;

        public ILLocation()
        {
        }

        public ILLocation(int position)
        {
            _position = position;
        }

        public void SetPosition(int position)
        {
            _position = position;
        }

        /// <summary>
        /// Adjust our position by a certain amount of long branches
        /// </summary>
        /// <param name="longBranchCount"></param>
        /// <remarks></remarks>
        public void AdjustForLongBranch(int longBranchCount)
        {
            _position += longBranchCount * LongBranchAdjust;
        }

        /// <summary>
        /// Determine if this branch is long
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public bool IsLongBranch(ILLocation target)
        {
            // The branch offset is relative to the instruction *after* the branch so we add 2 (length of a br_s) to our position
            return Utility.IsLongBranch(_position + BrSLength, target._position);
        }

        public bool Equals1(ILLocation other)
        {
            return _position == other._position;
        }
        bool System.IEquatable<ILLocation>.Equals(ILLocation other)
        {
            return Equals1(other);
        }

        public override string ToString()
        {
            return _position.ToString("x");
        }

        public int CompareTo(ILLocation other)
        {
            return _position.CompareTo(other._position);
        }
    }

    [Obsolete("Represents a branch from a start location to an end location")]
    internal class BranchInfo 
    {
        private readonly ILLocation _start;
        private readonly ILLocation _end;
        private Label _label;
        private bool _isLongBranch;

        public BranchInfo(ILLocation startLocation, Label endLabel)
        {
            _start = startLocation;
            _label = endLabel;
            _end = new ILLocation();
        }

        public void AdjustForLongBranches(int longBranchCount)
        {
            _start.AdjustForLongBranch(longBranchCount);
            // end not necessarily needed once we determine
            // if this is long, but keep it accurate anyway.
            _end.AdjustForLongBranch(longBranchCount);
        }

        public void BakeIsLongBranch()
        {
            _isLongBranch = this.ComputeIsLongBranch();
        }

        public void AdjustForLongBranchesBetween(int betweenLongBranchCount)
        {
            _end.AdjustForLongBranch(betweenLongBranchCount);
        }

        public bool IsBetween(BranchInfo other)
        {
            return _start.CompareTo(other._start) > 0 && _start.CompareTo(other._end) < 0;
        }

        public bool ComputeIsLongBranch()
        {
            return _start.IsLongBranch(_end);
        }

        public void Mark(Label target, int position)
        {
            if (_label.Equals(target) == true)
            {
                _end.SetPosition(position);
            }
        }

        /// <summary>
        /// We only need to compare the start point. Can only have a single
        /// brach from the exact address, so if label doesn't match we have
        /// bigger problems.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(ILLocation start)
        {
            return _start.Equals1(start);
        }

        public override string ToString()
        {
            return $"{_start} -> {_end} (L={_start.IsLongBranch(_end)})";
        }

        public bool IsLongBranch => _isLongBranch;
    }
}
