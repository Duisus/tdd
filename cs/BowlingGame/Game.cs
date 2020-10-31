using System;
using System.Collections.Generic;
using System.Linq;
using BowlingGame.Infrastructure;
using FluentAssertions;
using NUnit.Framework;

namespace BowlingGame
{
    public class Game
    {
        private List<Frame> Frames = new List<Frame>();

        public Game()
        {
            Frames.Add(new Frame());
        }
        public void Roll(int pins)
        {
            var lastIsSpecialRoll = false;
            if (!Frames.Last().AddRoll(pins))
            {
                var currentFrame = new Frame(pins);
                lastIsSpecialRoll = Frames.Last().HasBonus;
                Frames.Last().NextFrame = currentFrame;
                Frames.Add(currentFrame);
            }
            if(Frames.Count==11 && !lastIsSpecialRoll) 
                throw new Exception();
        }

        public int GetScore()
        {
            int result = 0;
            Frames.ForEach(frame => frame.CalculateBonus());
            foreach (var frame in Frames)
            {
                result += frame.Result;
            }

            return result;
        }
        
        private class Frame
        {
            private int? firstRoll;
            public int? FirstRoll
            {
                get
                {
                    return firstRoll;
                }

                set
                {
                    if (value > 10)
                        throw new Exception();
                    firstRoll = value;
                }
            }

            private int? secondRoll;
            public int? SecondRoll
            {
                get
                {
                    return secondRoll;
                }
                
                set
                {
                    if (value + FirstRoll > 10)
                        throw new Exception();
                    secondRoll = value;
                }
            }
            public int? Bonus;
            public Frame NextFrame;

            public int Result
            {
                get
                {
                    var scores = FirstRoll ?? 0;
                    scores += SecondRoll ?? 0;
                    scores += Bonus ?? 0;
                    return scores;
                }
            }
            
            public bool HasBonus => FirstRoll + SecondRoll == 10;

            public Frame(int pins)
            {
                AddRoll(pins);
            }
            public Frame() { }

            public bool AddRoll(int pins)
            {
                if (pins == 10) SecondRoll = 0;
                if (FirstRoll == null)
                    FirstRoll = pins;
                else if (SecondRoll == null)
                    SecondRoll = pins;
                else return false;
                return true;
            }

            public void CalculateBonus()
            {
                if (FirstRoll == 10)
                {
                    if(NextFrame == null)
                        return;
                    Bonus = NextFrame.FirstRoll;
                    if (NextFrame.FirstRoll != 10)
                        Bonus += NextFrame.SecondRoll;
                    else if (NextFrame.NextFrame != null)
                        Bonus += NextFrame.NextFrame.FirstRoll;
                }
                else if (FirstRoll + SecondRoll == 10)
                    Bonus = NextFrame.FirstRoll;
                else
                    Bonus = 0;
            }
        }
    }

    [TestFixture]
    public class Game_should : ReportingTest<Game_should>
    {
        [Test]
        public void HaveZeroScore_BeforeAnyRolls()
        {
            new Game()
                .GetScore()
                .Should().Be(0);
        }

        [Test]
        public void GetScore_IsJustSumOfShotedPins_WhenDoesNotHaveSpareOrStrike()
        {
            TestGetScore(expectedScore: 7, 3, 4);
        }
        
        [Test]
        public void GetScore_WhenStrikeBonusScoreShouldBeAdded()
        {
            TestGetScore(expectedScore: 18, 10, 2, 2);
        }

        [Test]
        public void GetScore_WhenWasSpare_AddPinsFromNextRollAsBonus()
        {
            TestGetScore(expectedScore: 16, 5, 5, 2, 2);
        }
        
        [Test]
        public void GetScore_WhenRollsInOtherFrameDontDoSpare()
        {
            TestGetScore(expectedScore: 16, 4, 4, 6, 2);
        }
        
        [Test]
        public void GetScore_WhenManyStrikeInARow()
        {
            TestGetScore(expectedScore: 40, 
                10,
                10,
                2, 2);
        }
        
        [Test]
        public void GetScore_WhenLastFrameContainsSpare()
        {
            TestGetScore(expectedScore: 20, 
                0,0,
                0,0,
                0,0,
                0,0,
                0,0,
                0,0,
                0,0,
                0,0,
                0,0,
                1,9,5);
        }
        
        [Test]
        public void GetScore_WhenPerfectGame()
        {
            TestGetScore(expectedScore: 300, 10, 10, 10, 10, 10, 10,
                10, 10, 10, 10, 10, 10);
        }

        [Test]
        public void Roll_IfInFrameMoreThan10Pins_ThrowException()
        {
            var game = new Game();

            game.Roll(9);

            Action act = () => game.Roll(2);
            act.Should().ThrowExactly<Exception>();
        }

        [Test]
        public void Roll_IfWereMoreThan10Frames_ThrowException()
        {
            var game = new Game();

            for (int i = 0; i < 10; i++)
            {
                game.Roll(2);
                game.Roll(2);
            }

            Action act = () => game.Roll(2);
            act.Should().ThrowExactly<Exception>();
        }

        private static void TestGetScore(int expectedScore, params int[] rolls)
        {
            var game = new Game();

            foreach (var pins in rolls)
            {
                game.Roll(pins);
            }
            var result = game.GetScore();

            result.Should().Be(expectedScore);
        }
    }
}