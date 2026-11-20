
using System;
using NUnit.Framework;

namespace LibertyHell.GameLoop
{
    public class RunParametersTests
    {
        // Проверяем создание объекта с корректными параметрами.
        [Test]
        public void Constructor_ValidParameters_CreatesObject()
        {
            var parameters = new RunParameters(100, 50, 100, 500, 60f);

            Assert.AreEqual(100, parameters.StartingMoney);
            Assert.AreEqual(50, parameters.StartingBlood);
            Assert.AreEqual(100, parameters.MaxBlood);
            Assert.AreEqual(500, parameters.MoneyGoal);
            Assert.AreEqual(60f, parameters.DurationSeconds);
            Assert.AreEqual(1, parameters.BloodRegenAmount);
            Assert.AreEqual(5f, parameters.BloodRegenIntervalSeconds);
        }

        // Проверяем отрицательное начальное количество денег.
        [Test]
        public void Constructor_NegativeStartingMoney_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new RunParameters(-1, 50, 100, 500, 60f));
        }

        // Проверяем нулевое максимальное количество крови.
        [Test]
        public void Constructor_InvalidMaxBlood_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new RunParameters(100, 50, 0, 500, 60f));
        }

        // Проверяем начальное количество крови выше максимума.
        [Test]
        public void Constructor_InvalidStartingBlood_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new RunParameters(100, 101, 100, 500, 60f));
        }

        // Проверяем нулевую цель по деньгам.
        [Test]
        public void Constructor_InvalidMoneyGoal_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new RunParameters(100, 50, 100, 0, 60f));
        }

        // Проверяем нулевую продолжительность запуска.
        [Test]
        public void Constructor_InvalidDuration_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new RunParameters(100, 50, 100, 500, 0f));
        }

        // Проверяем отрицательное количество восстанавливаемой крови.
        [Test]
        public void Constructor_NegativeBloodRegenAmount_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new RunParameters(100, 50, 100, 500, 60f, -1));
        }

        // Проверяем нулевой интервал восстановления крови.
        [Test]
        public void Constructor_InvalidBloodRegenInterval_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new RunParameters(100, 50, 100, 500, 60f, 1, 0f));
        }
    }
}
