using System;
using LostDungeon.Core.Pooling;
using NUnit.Framework;

namespace LostDungeon.Tests
{
    public class ObjectPoolTests
    {
        private class Bullet { }

        [Test]
        public void Get_ReusesReleasedObject()
        {
            var pool = new ObjectPool<Bullet>(() => new Bullet());
            Bullet first = pool.Get();
            pool.Release(first);

            Bullet second = pool.Get();

            Assert.AreSame(first, second);
        }

        [Test]
        public void Get_CreatesOnlyWhenEmpty()
        {
            int created = 0;
            var pool = new ObjectPool<Bullet>(() => { created++; return new Bullet(); });

            for (int i = 0; i < 5; i++)
                pool.Release(pool.Get());

            Assert.AreEqual(1, created);
        }

        [Test]
        public void Counts_TrackActiveAndInactive()
        {
            var pool = new ObjectPool<Bullet>(() => new Bullet());
            Bullet a = pool.Get();
            pool.Get();

            pool.Release(a);

            Assert.AreEqual(1, pool.CountActive);
            Assert.AreEqual(1, pool.CountInactive);
            Assert.AreEqual(2, pool.CountAll);
        }

        [Test]
        public void Release_Twice_Throws()
        {
            var pool = new ObjectPool<Bullet>(() => new Bullet());
            Bullet bullet = pool.Get();
            pool.Release(bullet);

            Assert.Throws<InvalidOperationException>(() => pool.Release(bullet));
        }

        [Test]
        public void Release_WhenFull_DestroysExtra()
        {
            int destroyed = 0;
            var pool = new ObjectPool<Bullet>(() => new Bullet(), onDestroy: _ => destroyed++, maxSize: 1);
            Bullet a = pool.Get();
            Bullet b = pool.Get();

            pool.Release(a);
            pool.Release(b);

            Assert.AreEqual(1, pool.CountInactive);
            Assert.AreEqual(1, destroyed);
        }

        [Test]
        public void Prewarm_FillsPoolWithoutLeavingAnythingActive()
        {
            var pool = new ObjectPool<Bullet>(() => new Bullet());

            pool.Prewarm(10);

            Assert.AreEqual(10, pool.CountInactive);
            Assert.AreEqual(0, pool.CountActive);
        }
    }
}
