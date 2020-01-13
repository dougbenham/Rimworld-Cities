﻿using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Cities
{
	public class City : Settlement {
		public Faction inhabitantFaction;

		public bool Abandoned => inhabitantFaction == null;

		public override MapGeneratorDef MapGeneratorDef => def.mapGenerator;

		public override Texture2D ExpandingIcon => def.ExpandingIconTexture;

		public override Color ExpandingIconColor => inhabitantFaction != null ? base.ExpandingIconColor : Color.grey;

		public override bool Visitable => base.Visitable || Abandoned;

		public override bool Attackable => base.Attackable && !Abandoned;

		public IEnumerable<Quest> QuestsHere => Find.World.GetComponent<WorldComponent_QuestTracker>().quests
			.Where(q => q.Targets.targets.Contains(this));

		bool QuestTab_IsQuest => QuestsHere.Any();
		//string QuestTab_Label => QuestsHere.Select(q => q.Name).ToArray().ToCommaList();
		string QuestTab_Label => Name;
		int QuestTab_TicksLeft => QuestsHere.Where(q => q.TicksLeft > 0).TryMinBy(q => q.TicksLeft, out var min) ? min.TicksLeft : -1;
		int QuestTab_Hostility => Faction.IsPlayer ? -1000 : -Faction.PlayerGoodwill;

		public override void SetFaction(Faction newFaction) {
			base.SetFaction(newFaction);
			if(inhabitantFaction != null) {
				inhabitantFaction = newFaction;
			}
		}

		public override void PostMake() {
			base.PostMake();
			if(Abandoned) {
				trader = null;
			}
		}

		public override void PostMapGenerate() {
			base.PostMapGenerate();
			if(Abandoned) {
				SetFaction(Faction.OfPlayer);
			}
		}

		public override IEnumerable<Gizmo> GetCaravanGizmos(Caravan caravan) {
			if(Visitable) {
				var action = new Command_Action {
					icon = SettleUtility.SettleCommandTex,
					defaultLabel = "EnterCity".Translate(),
					defaultDesc = "EnterCityDesc".Translate(),
					action = () => {
						LongEventHandler.QueueLongEvent(() => {
							var orGenerateMap = GetOrGenerateMapUtility.GetOrGenerateMap(Tile, null);
							CaravanEnterMapUtility.Enter(caravan, orGenerateMap, CaravanEnterMode.Edge, CaravanDropInventoryMode.DoNotDrop, draftColonists: true);
						}, "GeneratingMapForNewEncounter", false, null);
					},
				};
				if(this.EnterCooldownBlocksEntering()) {
					action.disabled = true;
					action.disabledReason = "MessageEnterCooldownBlocksEntering".Translate(this.EnterCooldownDaysLeft().ToString("0.#"));
				}
				yield return action;
			}
			foreach(var gizmo in base.GetCaravanGizmos(caravan)) {
				yield return gizmo;
			}
		}

		public override void ExposeData() {
			Scribe_References.Look(ref inhabitantFaction, "inhabitantFaction");
			base.ExposeData();
		}

		public virtual void PreMapGenerate(Map map) {
			SetFaction(inhabitantFaction ?? GenCity.RandomCityFaction(f => f.HostileTo(Faction.OfPlayer)));
		}

		public override string GetInspectString() {
			var s = base.GetInspectString();
			bool hasQuests = false;
			foreach(var quest in QuestsHere) {
				if(!hasQuests) {
					hasQuests = true;
					s += "\n--";
				}
				var ticksLeft = quest.TicksLeft;
				if(ticksLeft > 0) {
					s += "\n" + quest.Name + " (" + ticksLeft.ToStringTicksToPeriod() + ")";
				}
			}
			return s;
		}
	}
}