using System.Reflection;
using NLog;
using Sandbox.Game.Entities;
using Torch.Managers.PatchManager;

namespace TorchAlert.Core.Patches
{
    [PatchShim]
    public static class MyCubeGridPatch
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        public static ParentsLookupTree<long> SplitLookup { private get; set; }

        public static void Patch(PatchContext ptx)
        {
            var patchee = typeof(MyCubeGrid).GetMethod("MoveBlocks", BindingFlags.Static | BindingFlags.NonPublic);
            var patcher = typeof(MyCubeGridPatch).GetMethod(nameof(MoveBlocksPostfix), BindingFlags.Static | BindingFlags.NonPublic);
            ptx.GetPattern(patchee).Suffixes.Add(patcher);
        }

        static void MoveBlocksPostfix(ref MyCubeGrid from, ref MyCubeGrid to)
        {
            SplitLookup?.Add(from.EntityId, to.EntityId);
            Log.Trace($"split: \"{from.DisplayName}\" -> \"{to.DisplayName}\"");
        }
    }
}