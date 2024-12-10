using System.Collections.Generic;
using System.Windows;

namespace Hercules.Diagrams
{
    public static class Archetypes
    {
        public static Archetype GetArchetype(string archetypeName)
        {
            return DiagramBlockInfo.GetValueOrDefault(archetypeName) ?? Invalid;
        }

        public static readonly Archetype Invalid = new Archetype(ArchetypeType.Invalid, new Size(80, 80), Fugue.Icons.Prohibition, (prototype, element) => new BlockInvalid(prototype, element));

        private static readonly Dictionary<string, Archetype> DiagramBlockInfo = new()
        {
            { "start", new Archetype(ArchetypeType.Start, new Size(60, 45), Fugue.Icons.Flag, (prototype, element) => new BlockStart(prototype, element)) },
            { "success", new Archetype(ArchetypeType.Success, new Size(60, 45), Fugue.Icons.SmileyMrGreen, (prototype, element) => new BlockSuccess(prototype, element)) },
            { "failure", new Archetype(ArchetypeType.Failure, new Size(60, 45), Fugue.Icons.SmileySad, (prototype, element) => new BlockFailure(prototype, element)) },
            { "spawn", new Archetype(ArchetypeType.Spawn, new Size(225, 125), Fugue.Icons.PlusOctagon, (prototype, element) => new BlockSpawn(prototype, element)) },
            { "objective", new Archetype(ArchetypeType.Objective, new Size(225, 125), Fugue.Icons.Star, (prototype, element) => new BlockObjective(prototype, element)) },
            { "dialog", new Archetype(ArchetypeType.Dialog, new Size(225, 125), Fugue.Icons.BalloonBox, (prototype, element) => new BlockDialog(prototype, element)) },
            { "invalid", Invalid },
            { "asset", new Archetype(ArchetypeType.Asset, new Size(225, 125), Fugue.Icons.PaperClip, (prototype, element) => new BlockAsset(prototype, element)) },
            { "random", new Archetype(ArchetypeType.Random, new Size(75, 50), Fugue.Icons.Block, (prototype, element) => new BlockRandom(prototype, element)) },
            { "helper", new Archetype(ArchetypeType.Helper, new Size(225, 125), Fugue.Icons.Hammer, (prototype, element) => new BlockHelper(prototype, element)) },
            { "timer", new Archetype(ArchetypeType.Timer, new Size(75, 75), Fugue.Icons.ClockSelect, (prototype, element) => new BlockTimer(prototype, element)) },

            { "penta_down", new Archetype(ArchetypeType.PentaDown, new Size(125, 100), null, (prototype, element) => new BlockPentaDown(prototype, element)) },
            { "penta_left", new Archetype(ArchetypeType.PentaLeft, new Size(125, 100), null, (prototype, element) => new BlockPentaLeft(prototype, element)) },
            { "penta_right", new Archetype(ArchetypeType.PentaRight, new Size(125, 100), null, (prototype, element) => new BlockPentaRight(prototype, element)) },
            { "penta_up", new Archetype(ArchetypeType.PentaUp, new Size(125, 100), null, (prototype, element) => new BlockPentaUp(prototype, element)) },
            { "rhomb", new Archetype(ArchetypeType.Rhomb, new Size(125, 100), null, (prototype, element) => new BlockRhomb(prototype, element)) },
            { "hexagon", new Archetype(ArchetypeType.Hexagon, new Size(100, 110), null, (prototype, element) => new BlockHexagon(prototype, element)) },
            { "trapeze", new Archetype(ArchetypeType.Trapeze, new Size(125, 100), null, (prototype, element) => new BlockTrapeze(prototype, element)) },
            { "barrel", new Archetype(ArchetypeType.Barrel, new Size(125, 100), null, (prototype, element) => new BlockBarrel(prototype, element)) },
            { "box", new Archetype(ArchetypeType.Box, new Size(125, 100), null, (prototype, element) => new BlockBox(prototype, element)) },

            { "penta_right_lite", new Archetype(ArchetypeType.LitePentaRight, new Size(45, 60), null, (prototype, element) => new BlockPentaRightLite(prototype, element)) },
            { "penta_down_lite", new Archetype(ArchetypeType.LitePentaDown, new Size(60, 45), null, (prototype, element) => new BlockPentaDownLite(prototype, element)) },
            { "penta_left_lite", new Archetype(ArchetypeType.LitePentaLeft, new Size(45, 60), null, (prototype, element) => new BlockPentaLeftLite(prototype, element)) },
            { "penta_up_lite", new Archetype(ArchetypeType.LitePentaUp, new Size(60, 45), null, (prototype, element) => new BlockPentaUpLite(prototype, element)) },
            { "rhomb_lite", new Archetype(ArchetypeType.LiteRhomb, new Size(70, 70), null, (prototype, element) => new BlockRhombLite(prototype, element)) },
            { "hexagon_lite", new Archetype(ArchetypeType.LiteHexagon, new Size(70, 77), null, (prototype, element) => new BlockHexagonLite(prototype, element)) },
            { "trapeze_lite", new Archetype(ArchetypeType.LiteTrapeze, new Size(75, 50), null, (prototype, element) => new BlockTrapezeLite(prototype, element)) },
            { "barrel_lite", new Archetype(ArchetypeType.LiteBarrel, new Size(70, 70), null, (prototype, element) => new BlockBarrelLite(prototype, element)) },
            { "box_lite", new Archetype(ArchetypeType.LiteBox, new Size(70, 70), null, (prototype, element) => new BlockBoxLite(prototype, element)) },

            { "window", new Archetype(ArchetypeType.Window, new Size(225, 125), null, (prototype, element) => new BlockWindow(prototype, element)) },

            { "root", new Archetype(ArchetypeType.AiRoot, new Size(75, 60), Fugue.Icons.ArrowSplit270, (prototype, element) => new BlockRoot(prototype, element)) },
            { "sequence", new Archetype(ArchetypeType.AiSequence, new Size(75, 60), Fugue.Icons.ArrowSkip, (prototype, element) => new BlockSequence(prototype, element)) },
            { "selector", new Archetype(ArchetypeType.AiSelector, new Size(75, 60), Fugue.Icons.ArrowBranch, (prototype, element) => new BlockSelector(prototype, element)) },
            { "decorator", new Archetype(ArchetypeType.AiDecorator, new Size(110, 75), Fugue.Icons.PaintCan, (prototype, element) => new BlockDecorator(prototype, element)) },
            { "behaviour", new Archetype(ArchetypeType.AiBehaviour, new Size(115, 90), Fugue.Icons.EditCode, (prototype, element) => new BlockBehaviour(prototype, element)) },
            { "interceptor", new Archetype(ArchetypeType.AiInterceptor, new Size(75, 60), Fugue.Icons.Lightning, (prototype, element) => new BlockInterceptor(prototype, element)) },
            { "condition", new Archetype(ArchetypeType.AiCondition, new Size(115, 90), Fugue.Icons.Question, (prototype, element) => new BlockCondition(prototype, element)) },
            { "check", new Archetype(ArchetypeType.AiCheck, new Size(80, 80), Fugue.Icons.AddressBookArrow, (prototype, element) => new BlockCheck(prototype, element)) },
            { "parallel", new Archetype(ArchetypeType.AiParallel, new Size(75, 60), Fugue.Icons.AnimalDog, (prototype, element) => new BlockParallel(prototype, element)) }
        };
    }
}