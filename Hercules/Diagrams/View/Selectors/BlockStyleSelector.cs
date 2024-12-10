using System.Windows;
using System.Windows.Controls;

namespace Hercules.Diagrams.View
{
    public class BlockStyleSelector : StyleSelector
    {
        public override Style SelectStyle(object item, DependencyObject container)
        {
            var viewModel = item as BlockBase;

            if (viewModel == null)
                return base.SelectStyle(item, container);

            return SelectStyle(viewModel.Type);
        }

        private Style SelectStyle(ArchetypeType archetypeType)
        {
            Style resultStyle;
            switch (archetypeType)
            {
                case ArchetypeType.Start:
                    resultStyle = (Style)Application.Current.FindResource("BlockStartStyle");
                    break;
                case ArchetypeType.Success:
                    resultStyle = (Style)Application.Current.FindResource("BlockFinishStyle");
                    break;
                case ArchetypeType.Failure:
                    resultStyle = (Style)Application.Current.FindResource("BlockFinishStyle");
                    break;
                case ArchetypeType.Random:
                    resultStyle = (Style)Application.Current.FindResource("BlockRandomStyle");
                    break;
                case ArchetypeType.Timer:
                    resultStyle = (Style)Application.Current.FindResource("BlockTimerStyle");
                    break;
                case ArchetypeType.AiRoot:
                    resultStyle = (Style)Application.Current.FindResource("BlockRhombusStyle");
                    break;
                case ArchetypeType.AiSequence:
                    resultStyle = (Style)Application.Current.FindResource("BlockRhombusStyle");
                    break;
                case ArchetypeType.AiBehaviour:
                    resultStyle = (Style)Application.Current.FindResource("BlockBehaviourStyle");
                    break;
                case ArchetypeType.AiDecorator:
                    resultStyle = (Style)Application.Current.FindResource("BlockDecaratorStyle");
                    break;
                case ArchetypeType.AiSelector:
                    resultStyle = (Style)Application.Current.FindResource("BlockRhombusStyle");
                    break;
                case ArchetypeType.AiCondition:
                    resultStyle = (Style)Application.Current.FindResource("BlockConditionStyle");
                    break;
                case ArchetypeType.Invalid:
                    resultStyle = (Style)Application.Current.FindResource("BlockInvalidStyle");
                    break;
                case ArchetypeType.Asset:
                    resultStyle = (Style)Application.Current.FindResource("BlockAssetStyle");
                    break;
                case ArchetypeType.AiCheck:
                    resultStyle = (Style)Application.Current.FindResource("BlockCheckStyle");
                    break;
                case ArchetypeType.AiParallel:
                    resultStyle = (Style)Application.Current.FindResource("BlockRhombusStyle");
                    break;
                case ArchetypeType.AiInterceptor:
                    resultStyle = (Style)Application.Current.FindResource("BlockRhombusStyle");
                    break;
                /*lite and common Archetype*/
                case ArchetypeType.LitePentaDown:
                case ArchetypeType.LitePentaLeft:
                case ArchetypeType.LitePentaRight:
                case ArchetypeType.LitePentaUp:
                case ArchetypeType.LiteRhomb:
                case ArchetypeType.LiteBarrel:
                case ArchetypeType.LiteBox:
                case ArchetypeType.LiteHexagon:
                case ArchetypeType.LiteTrapeze:
                    resultStyle = (Style)Application.Current.FindResource("BlockCommonLiteStyle");
                    break;
                case ArchetypeType.PentaDown:
                case ArchetypeType.PentaLeft:
                case ArchetypeType.PentaRight:
                case ArchetypeType.PentaUp:
                case ArchetypeType.Rhomb:
                case ArchetypeType.Barrel:
                case ArchetypeType.Box:
                case ArchetypeType.Hexagon:
                case ArchetypeType.Trapeze:
                    resultStyle = (Style)Application.Current.FindResource("BlockCommonStyle");
                    break;
                case ArchetypeType.Window:
                    resultStyle = (Style)Application.Current.FindResource("BlockWindowStyle");
                    break;
                default:
                    resultStyle = (Style)Application.Current.FindResource("BlockWindowStyle");
                    break;
            }

            return resultStyle;
        }
    }
}