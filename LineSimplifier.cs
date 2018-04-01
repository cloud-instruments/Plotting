/*
Copyright(c) <2018> <University of Washington>
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using Priority_Queue;

namespace Plotting
{
    public class LineSimplifier
    {
        #region Private types

        public class Node : FastPriorityQueueNode
        {
            public int Index { get; set; }

            public Point2Y PrevVertext { get; set; }

            public Point2Y CurrentVertext { get; set; }

            public Point2Y NextVertext { get; set; }

            public Point2Y Point { get; set; }

            public Node Prev { get; set; }
            public Node Next { get; set; }
        }

        #endregion

        #region Public methods

        public List<Point> Simplify(List<Point> points, int maxLength)
        {
            if (maxLength < 2)
                maxLength = 2;

            if (points.Count <= 2 || points.Count <= maxLength)
                return points;

            var queue = PopulateQueue(points);
            while (queue.Count > maxLength - 2)
                RemoveOne(queue);

            return queue
                .OrderBy(n => n.Index)
                .Select(n => n.CurrentVertext.ToPoint())
                .ToList();
        }

        public List<Point2Y> Simplify(List<Point2Y> points, int maxLength)
        {
            if (maxLength < 2)
                maxLength = 2;

            if (points.Count <= 2 || points.Count <= maxLength)
                return points;

            var queue = PopulateQueue2Y(points);

            while (queue.Count > maxLength)
                RemoveOne(queue);

            return queue
                .OrderBy(n => n.Index)
                .Select(n => n.Point)
                .ToList();
        }

        #endregion

        #region Private methods

        private static FastPriorityQueue<Node> PopulateQueue(List<Point> points)
        {
            var queue = new FastPriorityQueue<Node>(points.Count - 2);
            Node prev = null;

            for (var i = 1; i < points.Count - 1; i++)
            {
                var area = CalcTriangleArea(points[i - 1], points[i], points[i + 1]);
                var triangle = new Node
                {
                    Index = i,
                    Point = new Point2Y { X = points[i].X, Y1 = points[i].Y },

                    PrevVertext = new Point2Y { X = points[i - 1].X, Y1 = points[i - 1].Y },
                    CurrentVertext = new Point2Y { X = points[i].X, Y1 = points[i].Y },
                    NextVertext = new Point2Y { X = points[i + 1].X, Y1 = points[i + 1].Y },

                    Prev = prev
                };

                if (prev != null)
                    prev.Next = triangle;
                prev = triangle;
                queue.Enqueue(triangle, area);
            }

            return queue;
        }

        private static FastPriorityQueue<Node> PopulateQueue2(List<Point> points)
        {
            var queue = new FastPriorityQueue<Node>(points.Count);

            Node prev = null;
            for (var i = 0; i < points.Count; i++)
            {
                var area = i == 0 || i == points.Count - 1 ? float.MaxValue : CalcTriangleArea(points[i - 1], points[i], points[i + 1]);

                var node = new Node
                {
                    Index = i,
                    Point = new Point2Y { Y1 = points[i].Y, X = points[i].X },
                    Prev = prev
                };

                if (prev != null)
                    prev.Next = node;

                prev = node;
                queue.Enqueue(node, area);
            }

            return queue;
        }

        private static FastPriorityQueue<Node> PopulateQueue2Y(List<Point2Y> points)
        {
            var queue = new FastPriorityQueue<Node>(points.Count);

            Node prev = null;
            for (var i = 0; i < points.Count; i++)
            {
                var area = i == 0 || i == points.Count - 1 ? float.MaxValue : CalcArea2Y(points[i - 1], points[i], points[i + 1]);

                var node = new Node
                {
                    Index = i,
                    Point = points[i],
                    Prev = prev
                };

                if (prev != null)
                    prev.Next = node;

                prev = node;
                queue.Enqueue(node, area);
            }

            return queue;
        }


        private static void Update(FastPriorityQueue<Node> queue, Node node, float minArea)
        {
            //var area = node.Prev == null || node.Next == null ? float.MaxValue : CalcTriangleArea(node.PrevVertext.ToPoint(), node.CurrentVertext.ToPoint(), node.NextVertext.ToPoint());
            //if (area < minArea)
            //    area = minArea;
            //queue.UpdatePriority(node, area);

            queue.Remove(node);
            queue.Enqueue(node, CalcTriangleArea(node.PrevVertext.ToPoint(), node.CurrentVertext.ToPoint(), node.NextVertext.ToPoint()));
        }

        private static void RemoveOne(FastPriorityQueue<Node> queue)
        {
            var triangle = queue.Dequeue();
            var prevTriangle = triangle.Prev;
            var nextTriangle = triangle.Next;

            if (prevTriangle != null)
            {
                prevTriangle.Next = triangle.Next;
                prevTriangle.NextVertext = triangle.NextVertext;
                Update(queue, prevTriangle, triangle.Priority);
                //node.Next.Prev = node.Prev;
                //Update(queue, node.Next, node.Priority);
            }

            if (nextTriangle != null)
            {
                nextTriangle.Prev = triangle.Prev;
                nextTriangle.PrevVertext = triangle.PrevVertext;
                Update(queue, nextTriangle, triangle.Priority);
                //node.Prev.Next = node.Next;
                //Update(queue, node.Prev, node.Priority);
            }
        }

        #endregion

        #region Private methods

        private static float CalcTriangleArea(Point a, Point b, Point c)
        {
            //return (float)Math.Abs(a.X * b.Y + b.X * c.Y + c.X * a.Y - a.X * c.Y - b.X * a.Y - c.X * b.Y);
            return (float)(Math.Abs(a.X * (b.Y - c.Y) + b.X * (c.Y - a.Y) + c.X * (a.Y - b.Y)) / 2f);
        }


        private static float CalcArea2Y(Point2Y p1, Point2Y p2, Point2Y p3)
        {

            return (float)Math.Abs(p1.X * p2.Y1 + p2.X * p3.Y1 + p3.X * p1.Y1
                                + p1.X * p2.Y2 + p2.X * p3.Y2 + p3.X * p1.Y2 
                                - p1.X * p3.Y1 - p2.X * p1.Y1 - p3.X * p2.Y1
                                - p1.X * p3.Y2 - p2.X * p1.Y2 - p3.X * p2.Y2);
        }
        #endregion
    }
}
