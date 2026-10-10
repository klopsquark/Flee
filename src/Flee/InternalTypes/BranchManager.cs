using System.Reflection.Emit;

namespace Flee.InternalTypes
{
    /// <summary>
    /// Manages branch information and allows us to determine if we should emit a short or long branch
    /// </summary>
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
}
