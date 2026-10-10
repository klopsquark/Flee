using System.Reflection.Emit;

namespace Flee.InternalTypes
{
    /// <summary>
    /// Represents a branch from a start location to an end location
    /// </summary>
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
            if (_label.Equals(target))
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
