using Hercules.Forms.Elements;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;

namespace Hercules.Forms.Tests
{
    public class TestUndoStep
    {
        public Action<ITransaction> Undo { get; }
        public Action<ITransaction> Redo { get; }

        public TestUndoStep(Action<ITransaction> undo, Action<ITransaction> redo)
        {
            this.Undo = undo;
            this.Redo = redo;
        }
    }

    [TestFixture]
    public class UndoHistoryTests : IUndoHandler<TestUndoStep>
    {
        void DoNothing(ITransaction transaction)
        {
        }

        public void Undo(TestUndoStep step, ITransaction transaction)
        {
            step.Undo(transaction);
        }

        public void Redo(TestUndoStep step, ITransaction transaction)
        {
            step.Redo(transaction);
        }

        [Test]
        public void Clear()
        {
            var history = new UndoHistory<TestUndoStep>(this);
            history.Push(new TestUndoStep(DoNothing, DoNothing));
            history.Push(new TestUndoStep(DoNothing, DoNothing));
            history.Clear();
            ClassicAssert.False(history.CanUndo);
            ClassicAssert.False(history.CanRedo);
        }

        [Test]
        public void CanRedo_AfterUndo_True()
        {
            var history = new UndoHistory<TestUndoStep>(this);
            history.Push(new TestUndoStep(DoNothing, DoNothing));
            history.Undo(Transactions.CreateUndoRedoTransaction());
            ClassicAssert.True(history.CanRedo);
        }

        [Test]
        public void CanRedo_AfterPush_False()
        {
            var history = new UndoHistory<TestUndoStep>(this);
            history.Push(new TestUndoStep(DoNothing, DoNothing));
            history.Undo(Transactions.CreateUndoRedoTransaction());
            history.Push(new TestUndoStep(DoNothing, DoNothing));
            ClassicAssert.False(history.CanRedo);
        }

        [Test]
        public void Undo_Single()
        {
            var history = new UndoHistory<TestUndoStep>(this);
            int i = 1;
            history.Push(new TestUndoStep(_ => i = 0, _ => i = 1));
            history.Undo(Transactions.CreateUndoRedoTransaction());
            ClassicAssert.AreEqual(i, 0);
        }

        [Test]
        public void Redo_Single()
        {
            var history = new UndoHistory<TestUndoStep>(this);
            int i = 1;
            history.Push(new TestUndoStep(_ => i = 0, _ => i = 1));
            history.Undo(Transactions.CreateUndoRedoTransaction());
            history.Redo(Transactions.CreateUndoRedoTransaction());
            ClassicAssert.AreEqual(i, 1);
        }

        [Test]
        public void UndoRedoSequence()
        {
            int i = 0;
            var history = new UndoHistory<TestUndoStep>(this);
            history.Push(new TestUndoStep(_ => i = 0, _ => i = 1));
            history.Push(new TestUndoStep(_ => i = 1, _ => i = 2));
            history.Push(new TestUndoStep(_ => i = 2, _ => i = 3));
            i = 3;
            history.Undo(Transactions.CreateUndoRedoTransaction());
            Assert.That(i == 2);
            history.Undo(Transactions.CreateUndoRedoTransaction());
            Assert.That(i == 1);
            history.Undo(Transactions.CreateUndoRedoTransaction());
            Assert.That(i == 0);
            history.Redo(Transactions.CreateUndoRedoTransaction());
            Assert.That(i == 1);
            history.Push(new TestUndoStep(_ => { i = 1; }, _ => { i = 4; }));
            i = 4;
            history.Undo(Transactions.CreateUndoRedoTransaction());
            Assert.That(i == 1);
            history.Redo(Transactions.CreateUndoRedoTransaction());
            Assert.That(i == 4);
        }
    }
}