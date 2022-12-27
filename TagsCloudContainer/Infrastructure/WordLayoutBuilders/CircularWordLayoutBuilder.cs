﻿using EParser;
using FluentResults;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagsCloudVisualization;

namespace TagsCloudContainer.Infrastructure.WordLayoutBuilders
{
    public class CircularWordLayoutBuilder : IWordLayoutBuilder
    {
        private readonly List<(string Word, SizeF Size)> words = new List<(string Word, SizeF Size)>();

        public Result AddWord(string word, SizeF size)
        {
            return Result.OkIf(!size.IsEmpty, "Size can't be empty")
                         .Bind(() => Result.OkIf(words.All(pair => pair.Word != word), $"Word '{word}' has already added to builder"))
                         .OnSuccess(r => words.Add((word, size)));
        }

        public Result<WordRectangle[]> Build(PointF center)
        {
            var distanceFunction = GetDistanceFunction(center);
            
            var rectangles = new List<RectangleF>();
            var searchStartPoints = new HashSet<PointF>();
            var wordRectangles = new List<WordRectangle>();
            foreach(var word in words)
            {
                var rectangle = GetRectangle(center, distanceFunction, rectangles, word.Size, searchStartPoints);
                if (rectangle is null)
                    return Result.Fail<WordRectangle[]>($"Can't build rectangle for word '{word}'");

                var value = rectangle.Value;
                rectangles.Add(value);
                AddRectanglePointsTo(searchStartPoints, value);
                wordRectangles.Add(new WordRectangle() { Word = word.Word, Rectangle = value });
            }
            return Result.Ok(wordRectangles.ToArray());
        }

        private static Func GetDistanceFunction(PointF center)
        {
            return t => (t[0] - center.X) * (t[0] - center.X) +
                        (t[1] - center.Y) * (t[1] - center.Y);
        }

        private static RectangleF? GetRectangle(PointF center, Func distanceFunction, IEnumerable<RectangleF> rectangles, SizeF targetRectangleSize, HashSet<PointF> searchStartPoints)
        {
            if (!rectangles.Any())
                return BuildRectangle(center, targetRectangleSize);

            var penaltyFunctions = GetPenaltyFunctions(rectangles, targetRectangleSize);
            var rectangle = BuildNearestRectangle(distanceFunction, penaltyFunctions, rectangles, targetRectangleSize, searchStartPoints);
            return rectangle;
        }

        private static List<Func> GetPenaltyFunctions(IEnumerable<RectangleF> rectanges, SizeF targetRectangleSize)
        {
            var penaltyFunctions = new List<Func>();
            foreach (var rectangle in rectanges)
            {
                var location = new PointF(rectangle.X - targetRectangleSize.Width / 2,
                                          rectangle.Y - targetRectangleSize.Height / 2);

                var size = new SizeF(rectangle.Width + targetRectangleSize.Width,
                                     rectangle.Height + targetRectangleSize.Height);

                penaltyFunctions.Add(GetRectanglePenaltyFunction(new RectangleF(location, size)));
            }
            return penaltyFunctions;
        }

        private static Func GetRectanglePenaltyFunction(RectangleF rectangle)
        {
            var left = rectangle.Left;
            var right = rectangle.Right;
            var bottom = rectangle.Bottom;
            var top = rectangle.Top;

            return t =>
            {
                double maxX = Math.Max(left - t[0], t[0] - right);
                double maxY = Math.Max(top - t[1], t[1] - bottom);
                double penalty = Math.Min(0, Math.Max(maxX, maxY));
                if (penalty < 0)
                    penalty *= -1E+15;
                return penalty;
            };
        }

        private static RectangleF BuildRectangle(PointF center, SizeF size)
        {
            var location = new PointF(center.X - size.Width / 2, center.Y - size.Height / 2);
            return new RectangleF(location, size);
        }

        private static RectangleF? BuildNearestRectangle(Func distanceFunction, 
                                                        List<Func> penaltyFunctions,
                                                        IEnumerable<RectangleF> rectanges,
                                                        SizeF requiredRectangleSize,
                                                        HashSet<PointF> searchStartPoints)
        {
            RectangleF nearestRectangle = default;
            var minFunctionValue = double.MaxValue;
            foreach (var startPoint in searchStartPoints)
            {
                var currentCenter = PenaltyMethodFacade.Calculate(distanceFunction, startPoint, penaltyFunctions);
                var currentFunctionValue = distanceFunction(currentCenter.X, currentCenter.Y) +
                                           penaltyFunctions.Select(func => func(currentCenter.X, currentCenter.Y)).Sum();
                if (currentFunctionValue < minFunctionValue)
                {
                    var currentRectangle = BuildRectangle(currentCenter, requiredRectangleSize);
                    if (rectanges.Any(rectangle => rectangle.IntersectsWith(currentRectangle)))
                        continue;

                    minFunctionValue = currentFunctionValue;
                    nearestRectangle = currentRectangle;
                }
            }

            if (minFunctionValue == double.MaxValue)
                return null;

            return nearestRectangle;
        }

        private static void AddRectanglePointsTo(HashSet<PointF> points, RectangleF rectangle)
        {
            var halfWidth = rectangle.Width / 2;
            var halfHeight = rectangle.Height / 2;

            points.Add(rectangle.Location);
            points.Add(new PointF(rectangle.X + rectangle.Width, rectangle.Y));
            points.Add(new PointF(rectangle.X, rectangle.Y + rectangle.Height));
            points.Add(new PointF(rectangle.X + rectangle.Width, rectangle.Y + rectangle.Height));

            points.Add(new PointF(rectangle.X + halfWidth, rectangle.Y));
            points.Add(new PointF(rectangle.X, rectangle.Y + halfHeight));
            points.Add(new PointF(rectangle.X + rectangle.Width, rectangle.Y + halfHeight));
            points.Add(new PointF(rectangle.X + halfWidth, rectangle.Y + rectangle.Height));
        }

        public void Clear()
        {
            words.Clear();
        }
    }
}