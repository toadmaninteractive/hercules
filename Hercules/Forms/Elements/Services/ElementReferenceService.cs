using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Hercules.Forms.Elements
{
    public class ReferenceController
    {
        private readonly HashSet<RefElement> targetElements = new();
        private readonly HashSet<StringElement> sourceElements = new();
        private readonly HashSet<RefElement> newTargetElements = new();
        private readonly HashSet<StringElement> modifiedSourceElements = new();

        public IReadOnlyCollection<StringElement> SourceElements => sourceElements;
        public ObservableCollection<string> SourceValues { get; } = new();

        public bool Unique { get; private set; }

        public void RegisterSource(StringElement element, bool unique)
        {
            Unique = unique;
            if (element.IsActive)
                AddSourceElement(element);
            element.Events.OnActiveChanged += OnSourceActiveChanged;
        }

        public void RegisterTarget(RefElement refElement)
        {
            if (refElement.IsActive)
                AddTargetElement(refElement);
            refElement.Events.OnActiveChanged += OnTargetActiveChanged;
        }

        private void OnTargetActiveChanged(Element element, bool isActive, ITransaction transaction)
        {
            if (isActive)
                AddTargetElement((RefElement)element);
            else
                RemoveTargetElement((RefElement)element);
        }

        private void OnSourceActiveChanged(Element element, bool isActive, ITransaction transaction)
        {
            if (isActive)
                AddSourceElement((StringElement)element);
            else
                RemoveSourceElement((StringElement)element);
        }

        private void OnSourceValueChanged(Element element, ITransaction transaction)
        {
            modifiedSourceElements.Add((StringElement)element);
        }

        private void AddSourceElement(StringElement element)
        {
            sourceElements.Add(element);
            modifiedSourceElements.Add(element);
            element.Events.OnValueChanged += OnSourceValueChanged;
        }

        private void RemoveSourceElement(StringElement element)
        {
            element.Events.OnValueChanged -= OnSourceValueChanged;
            sourceElements.Remove(element);
            modifiedSourceElements.Add(element); // still mark as modified
        }

        private void AddTargetElement(RefElement element)
        {
            targetElements.Add(element);
            newTargetElements.Add(element);
        }

        private void RemoveTargetElement(RefElement element)
        {
            targetElements.Remove(element);
            newTargetElements.Remove(element);
        }

        public void ProcessTransaction(ITransaction transaction, TransactionStage stage)
        {
            switch (stage)
            {
                case TransactionStage.ContentUpdateFinalization:
                    if (modifiedSourceElements.Count > 0)
                    {
                        SourceValues.SynchronizeSorted(SourceElements.Select(s => s.Value).WhereNotNull().Order().Distinct());
                        foreach (var target in targetElements)
                        {
                            target.ReferenceSourceChanged(transaction);
                        }
                    }
                    else
                    {
                        foreach (var targetElement in newTargetElements)
                        {
                            targetElement.ReferenceSourceChanged(transaction);
                        }
                    }
                    break;

                case TransactionStage.ExternalValidation:
                    if (modifiedSourceElements.Count > 0 && Unique)
                    {
                        foreach (var group in SourceElements.GroupBy(element => element.Value))
                        {
                            var isValid = group.Count() == 1;
                            foreach (var element in group)
                            {
                                element.SetExternalValidationResult(isValid, transaction);
                            }
                        }
                    }
                    break;

                case TransactionStage.Cleanup:
                    modifiedSourceElements.Clear();
                    newTargetElements.Clear();
                    break;
            }
        }
    }

    public class ElementReferenceService : IFormService
    {
        private readonly Dictionary<string, ReferenceController> referenceControllers = new Dictionary<string, ReferenceController>();

        private ReferenceController GetController(string name) => referenceControllers.GetOrAdd(name, () => new ReferenceController());

        public void RegisterSource(string name, StringElement sourceElement, bool unique) => GetController(name).RegisterSource(sourceElement, unique);

        public ReferenceController RegisterTarget(string name, RefElement refElement)
        {
            var controller = GetController(name);
            controller.RegisterTarget(refElement);
            return controller;
        }

        public void ProcessTransaction(ITransaction transaction, TransactionStage stage)
        {
            foreach (var controller in referenceControllers)
            {
                controller.Value.ProcessTransaction(transaction, stage);
            }
        }
    }
}
