using System;
using System.Linq;
using MyNoSqlGrpc.Reader;
using MyNoSqlGrpc.Reader.Cache;
using NUnit.Framework;

namespace MyNoSqlGrpc.Tests
{
    public class TestDbRowDifferences
    {

        [Test]
        public void TestEmptyBeforeWeHaveARowAfter()
        {
            var partitionBefore = new ReaderPartition<int>();
            
            var partitionAfter = new ReaderPartition<int>();

            var readerRow = new ReaderRow<int>("TestRow", DateTime.UtcNow,  0);
            partitionAfter.Init(new []{readerRow});

            var result = partitionBefore.FindTheDifference(partitionAfter).ToList();
            
            Assert.AreEqual(RowOperationResult.Insert, result[0].Item1);
            Assert.AreEqual(1, result.Count);
        }
        
        [Test]
        public void TestWeHaveARowBeforeAndEmptyAfter()
        {
            var partitionBefore = new ReaderPartition<int>();
            
            var partitionAfter = new ReaderPartition<int>();

            var readerRow = new ReaderRow<int>("TestRow", DateTime.UtcNow,  0);
            partitionBefore.Init(new []{readerRow});

            var result = partitionBefore.FindTheDifference(partitionAfter).ToList();
            
            Assert.AreEqual(RowOperationResult.Delete, result[0].Item1);
            Assert.AreEqual(1, result.Count);
        }
        
        
        [Test]
        public void TestWeHaveSomeBeforeAndSomeAfter()
        {
            var partitionBefore = new ReaderPartition<int>();

            var dt1 = DateTime.UtcNow;
            var dt2 = DateTime.UtcNow.AddMinutes(1);
            var dt3 = DateTime.UtcNow.AddMinutes(2);
            
            var readerRow1 = new ReaderRow<int>("1", dt1,  1);
            var readerRow2 = new ReaderRow<int>("2", dt2,  2);
            partitionBefore.Init(new []{readerRow1, readerRow2});
            
            var readerRow3 = new ReaderRow<int>("3", dt3,  3);
            var partitionAfter = new ReaderPartition<int>();
            partitionAfter.Init(new[] {readerRow2, readerRow3});
            
            var result = partitionBefore.FindTheDifference(partitionAfter).ToList();
            
            Assert.AreEqual(2, result.Count);

            var insertedEntity = result.FirstOrDefault(itm => itm.Item1 == RowOperationResult.Insert);
            Assert.AreEqual(3, insertedEntity.Item2);
            
            var deletedEntity = result.FirstOrDefault(itm => itm.Item1 == RowOperationResult.Delete);
            Assert.AreEqual(1, deletedEntity.Item2);
        }

        [Test] 
        public void TestWeHaveSomeBeforeAndSomeAfterAndUpdatedInTheMiddle()
        {
            var partitionBefore = new ReaderPartition<int>();

            var dt1 = DateTime.UtcNow;
            var dt21 = DateTime.UtcNow.AddMinutes(1);
            var dt22 = DateTime.UtcNow.AddMinutes(2);
            var dt3 = DateTime.UtcNow.AddMinutes(3);
            
            var readerRow1 = new ReaderRow<int>("1", dt1,  1);
            var readerRow21 = new ReaderRow<int>("2", dt21,  2);
            partitionBefore.Init(new []{readerRow1, readerRow21});
            
            var readerRow22 = new ReaderRow<int>("2", dt22,  2);
            var readerRow3 = new ReaderRow<int>("3", dt3,  3);
            var partitionAfter = new ReaderPartition<int>();
            partitionAfter.Init(new[] {readerRow22, readerRow3});
            
            var result = partitionBefore.FindTheDifference(partitionAfter).ToList();
            
            Assert.AreEqual(3, result.Count);

            var insertedEntity = result.FirstOrDefault(itm => itm.Item1 == RowOperationResult.Insert);
            Assert.AreEqual(3, insertedEntity.Item2);
            
            var deletedEntity = result.FirstOrDefault(itm => itm.Item1 == RowOperationResult.Delete);
            Assert.AreEqual(1, deletedEntity.Item2);
            
            var updatedEntity = result.FirstOrDefault(itm => itm.Item1 == RowOperationResult.Update);
            Assert.AreEqual(2, updatedEntity.Item2);            
        }        
    }
}