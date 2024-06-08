// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using PawnOpinionCache
	= PerformanceFish.Cache.ByInt<Verse.Pawn, Verse.Pawn,
		PerformanceFish.PawnOpinionCaching.PawnOpinionCacheValue>;

namespace PerformanceFish;

public sealed class PawnOpinionCaching : ClassWithFishPatches
{
	public sealed class PawnOpinion : FirstPriorityFishPatch
	{
		public override string? Description { get; } = "Caches pawn opinion";

		public override MethodBase TargetMethodInfo { get; } = 
			AccessTools.DeclaredMethod(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.OpinionOf));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Prefix(Pawn_RelationsTracker __instance, Pawn other, ref int __result, out bool __state)
		{
			ref var cache = ref PawnOpinionCache.GetOrAddReference(__instance.pawn.thingIDNumber, other.thingIDNumber);

			if (cache.Dirty)
				return __state = true;

			__result = cache.Opinion;
			return __state = false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Postfix(Pawn_RelationsTracker __instance, Pawn other, int __result, bool __state)
		{
			if (!__state)
				return;

			UpdateCache(__instance.pawn, other, __result);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void UpdateCache(Pawn pawn, Pawn other, int __result)
			=> PawnOpinionCache.GetExistingReference(pawn.thingIDNumber, other.thingIDNumber)
				.Update(__result, pawn);
	}

	public record struct PawnOpinionCacheValue()
	{
		private int _nextRefreshTick = -2;
		public int Opinion;

		public void Update(int opinion, Pawn pawn)
		{
			Opinion = opinion;
			_nextRefreshTick = TickHelper.Add(GenTicks.TickRareInterval, pawn.thingIDNumber);
		}

		public bool Dirty
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => TickHelper.Past(_nextRefreshTick);
		}
	}
}