// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using LovePartnerCache
	= PerformanceFish.Cache.ByInt<Verse.Pawn, Verse.Pawn,
		PerformanceFish.LovePartnerCaching.LovePartnerCacheValue>;

namespace PerformanceFish;

public sealed class LovePartnerCaching : ClassWithFishPatches
{
	public sealed class LovePartner : FirstPriorityFishPatch
	{
		public override string? Description { get; } = "Caches most liked love partner";

		public override Delegate TargetMethodGroup { get; } = LovePartnerRelationUtility.ExistingMostLikedLovePartnerRel;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Prefix(Pawn p, bool allowDead, ref DirectPawnRelation __result, out bool __state)
		{
			ref var cache = ref LovePartnerCache.GetOrAddReference(p.thingIDNumber, allowDead.AsInt());

			if (cache.Dirty)
				return __state = true;

			__result = cache.Relation;
			return __state = false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Postfix(Pawn p, bool allowDead, DirectPawnRelation __result, bool __state)
		{
			if (!__state)
				return;

			UpdateCache(p, allowDead, __result);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void UpdateCache(Pawn pawn, bool allowDead, DirectPawnRelation __result)
			=> LovePartnerCache.GetExistingReference(pawn.thingIDNumber, allowDead.AsInt())
				.Update(__result, pawn);
	}

	public record struct LovePartnerCacheValue()
	{
		private int _nextRefreshTick = -2;
		public DirectPawnRelation Relation;

		public void Update(DirectPawnRelation relation, Pawn pawn)
		{
			Relation = relation;
			_nextRefreshTick = TickHelper.Add(GenTicks.TickLongInterval, pawn.thingIDNumber);
		}

		public bool Dirty
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => TickHelper.Past(_nextRefreshTick);
		}
	}
}