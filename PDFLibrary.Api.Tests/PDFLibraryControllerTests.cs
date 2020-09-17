using NUnit.Framework;
using PDFLibrary.Api.Controllers;
using Moq;
using PDFLibrary.Api.Services;
using Castle.Core.Logging;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using PDFLibrary.Api.Models;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace PDFLibrary.Api.Tests
{
    public class Tests
    {
        private Mock<IPDFStoreBlobStorage> _mockStorage;
        private Mock<ILogger<PDFLibraryController>> _mockLogger;
        const string TEST_FILENAME1 = "Test1.pdf";
        const string TEST_FILENAME2 = "Test2.pdf";

        [SetUp]
        public void Setup()
        {
            _mockStorage = new Mock<IPDFStoreBlobStorage>();
            _mockLogger = new Mock<ILogger<PDFLibraryController>>();
        }
        private List<PdfFileListItem> GetListOf2PdfFileListItems() => 
            new List<PdfFileListItem>() { new PdfFileListItem() { Name = TEST_FILENAME1 }, new PdfFileListItem() { Name = TEST_FILENAME2 } };
        private PdfFile GetTestPdfFile() => new PdfFile() { Name = TEST_FILENAME1, Content = new MemoryStream(new byte[] { 1 }), ContentType = "multipart/form" };

        [Test]
        public async Task GivenStoreHasPDFs_WhenGetCalled_ThenReturnTheListOfPdfDetails()
        {
            PDFLibraryController controller = GetController();

            _mockStorage.Setup(s => s.List()).
                Returns(Task.Factory.StartNew(() => GetListOf2PdfFileListItems()));

            var actualList = await controller.Get();

            var expectedList = GetListOf2PdfFileListItems();

            for (int i = 0; i < expectedList.Count; i++)
            {
                Assert.AreEqual(expectedList[i].Name, actualList[i].Name);
            }

            _mockStorage.Verify(s => s.List(), Times.Once);
        }

        [Test]
        public void GivenStoreThrows_WhenGetCalled_ThenReThrow()
        {
            PDFLibraryController controller = GetController();

            Exception thrown = new Exception();
            _mockStorage.Setup(s => s.List()).Throws(thrown);

            var ex = Assert.Throws<AggregateException>(() =>
            {
                var ret = controller.Get().Result;
            });

            Assert.AreEqual(thrown, ex.InnerException);
        }

        [Test]
        public async Task GivenStoreHasPDFs_WhenGetPdfCalled_ThenReturnThePdf()
        {
            PDFLibraryController controller = GetController();

            _mockStorage.Setup(s => s.Download(TEST_FILENAME1)).
                Returns(Task.Factory.StartNew(() => GetTestPdfFile()));

            _mockStorage.Setup(s => s.CheckExists(TEST_FILENAME1)).
                Returns(Task.Factory.StartNew(() => true));

            var result = await controller.Get(TEST_FILENAME1);

            Assert.AreEqual(TEST_FILENAME1, (result as FileStreamResult).FileDownloadName);

            _mockStorage.Verify(s => s.CheckExists(TEST_FILENAME1), Times.Once);
            _mockStorage.Verify(s => s.Download(TEST_FILENAME1), Times.Once);
        }

        [Test]
        public async Task GivenNullParameter_WhenGetPdfCalled_ThenReturnBadRequest()
        {
            PDFLibraryController controller = GetController();

            var result = await controller.Get(null);

            Assert.IsInstanceOf<BadRequestObjectResult>(result);

            _mockStorage.Verify(s => s.CheckExists(TEST_FILENAME1), Times.Never);
            _mockStorage.Verify(s => s.Download(TEST_FILENAME1), Times.Never);
        }

        [Test]
        public async Task GivenStoreDoesNotHavePdf_WhenGetPdfCalled_ThenReturnNotFound()
        {
            PDFLibraryController controller = GetController();

            _mockStorage.Setup(s => s.CheckExists(TEST_FILENAME1)).
                Returns(Task.Factory.StartNew(() => false));

            var result = await controller.Get(TEST_FILENAME1);

            Assert.IsInstanceOf<NotFoundResult>(result);

            _mockStorage.Verify(s => s.CheckExists(TEST_FILENAME1), Times.Once);
            _mockStorage.Verify(s => s.Download(TEST_FILENAME1), Times.Never);
        }

        [Test]
        public void GivenStoreThrowsOnDownload_WhenGetPdfCalled_ThenReThrow()
        {
            PDFLibraryController controller = GetController();

            _mockStorage.Setup(s => s.CheckExists(TEST_FILENAME1)).
                Returns(Task.Factory.StartNew(() => true));

            Exception thrown = new Exception();
            _mockStorage.Setup(s => s.Download(TEST_FILENAME1)).Throws(thrown);

            var ex = Assert.Throws<AggregateException>(() =>
            {
                var ret = controller.Get(TEST_FILENAME1).Result;
            });

            Assert.AreEqual(thrown, ex.InnerException);
        }

        [Test]
        public void GivenStoreThrowsOnCheckExists_WhenGetPdfCalled_ThenReThrow()
        {
            PDFLibraryController controller = GetController();

            Exception thrown = new Exception();
            _mockStorage.Setup(s => s.CheckExists(TEST_FILENAME1)).Throws(thrown);

            var ex = Assert.Throws<AggregateException>(() =>
            {
                var ret = controller.Get(TEST_FILENAME1).Result;
            });

            Assert.AreEqual(thrown, ex.InnerException);
        }

        [Test]
        public async Task GivenPdfNotInStore_WhenPostCalled_ThenReturnStoreThePdf()
        {
            PDFLibraryController controller = GetController();

            _mockStorage.Setup(s => s.CheckExists(TEST_FILENAME1)).
                Returns(Task.Factory.StartNew(() => false));

            var testPdfFile = GetTestPdfFile();

            FormFile formFile = GetFormFile(testPdfFile);

            var result = await controller.Post(formFile);

            Assert.IsInstanceOf<OkResult>(result);

            _mockStorage.Verify(s => s.CheckExists(TEST_FILENAME1), Times.Once);
            _mockStorage.Verify(s => s.Add(It.IsAny<PdfFile>()), Times.Once);
        }

        [Test]
        public async Task GivenNullFilePassed_WhenPostCalled_ThenReturnBadRequest()
        {
            PDFLibraryController controller = GetController();

            var result = await controller.Post(null);

            Assert.IsInstanceOf<BadRequestObjectResult>(result);

            _mockStorage.Verify(s => s.CheckExists(TEST_FILENAME1), Times.Never);
            _mockStorage.Verify(s => s.Add(It.IsAny<PdfFile>()), Times.Never);
        }

        [Test]
        public async Task GivenTooLargeFilePassed_WhenPostCalled_ThenReturnBadRequest()
        {
            PDFLibraryController controller = GetController();

            var result = await controller.Post(GetTooLargeFormFile());

            Assert.IsInstanceOf<BadRequestObjectResult>(result);

            _mockStorage.Verify(s => s.CheckExists(TEST_FILENAME1), Times.Never);
            _mockStorage.Verify(s => s.Add(It.IsAny<PdfFile>()), Times.Never);
        }

        [Test]
        public async Task GivenInvalidTypeFilePassed_WhenPostCalled_ThenReturnBadRequest()
        {
            PDFLibraryController controller = GetController();

            var result = await controller.Post(GetInvalidTypeFormFile());

            Assert.IsInstanceOf<BadRequestObjectResult>(result);

            _mockStorage.Verify(s => s.CheckExists(TEST_FILENAME1), Times.Never);
            _mockStorage.Verify(s => s.Add(It.IsAny<PdfFile>()), Times.Never);
        }

        [Test]
        public async Task GivenPdfInStore_WhenPostCalled_ThenReturnBadRequest()
        {
            PDFLibraryController controller = GetController();

            _mockStorage.Setup(s => s.CheckExists(TEST_FILENAME1)).
                Returns(Task.Factory.StartNew(() => true));

            var testPdfFile = GetTestPdfFile();

            FormFile formFile = GetFormFile(testPdfFile);

            var result = await controller.Post(formFile);

            Assert.IsInstanceOf<BadRequestObjectResult>(result);

            _mockStorage.Verify(s => s.CheckExists(TEST_FILENAME1), Times.Once);
            _mockStorage.Verify(s => s.Add(It.IsAny<PdfFile>()), Times.Never);
        }

        [Test]
        public void GivenStoreThrowsOnAdd_WhenPostCalled_ThenReThrow()
        {
            PDFLibraryController controller = GetController();

            Exception thrown = new Exception();
            var testPdfFile = GetTestPdfFile();
            FormFile formFile = GetFormFile(testPdfFile);
            _mockStorage.Setup(s => s.CheckExists(TEST_FILENAME1)).
                Returns(Task.Factory.StartNew(() => false));
            _mockStorage.Setup(s => s.Add(It.IsAny<PdfFile>())).Throws(thrown);

            var ex = Assert.Throws<AggregateException>(() =>
            {
                var ret = controller.Post(formFile).Result;
            });

            Assert.AreEqual(thrown, ex.InnerException);
        }

        [Test]
        public void GivenStoreThrowsOnCheckExists_WhenPostCalled_ThenReThrow()
        {
            PDFLibraryController controller = GetController();

            Exception thrown = new Exception();
            var testPdfFile = GetTestPdfFile();
            FormFile formFile = GetFormFile(testPdfFile);
            _mockStorage.Setup(s => s.CheckExists(testPdfFile.Name)).Throws(thrown);

            var ex = Assert.Throws<AggregateException>(() =>
            {
                var ret = controller.Post(formFile).Result;
            });

            Assert.AreEqual(thrown, ex.InnerException);
        }

        [Test]
        public async Task GivenPdfsInStore_WhenReOrderCalled_ReOrder()
        {
            PDFLibraryController controller = GetController();

            var reOrderList = GetListOf2PdfFileListItems().Select(pdf => pdf.Name).ToList();

            _mockStorage.Setup(s => s.ReOrder(reOrderList)).
                Returns(Task.Factory.StartNew(() => false));

            var result = await controller.Put(reOrderList);

            Assert.IsInstanceOf<OkResult>(result);

            _mockStorage.Verify(s => s.ReOrder(reOrderList), Times.Once);
        }

        [Test]
        public async Task GivenNullParameterPassed_WhenReOrderCalled_ReturnBadRequest()
        {
            PDFLibraryController controller = GetController();

            var reOrderList = GetListOf2PdfFileListItems().Select(pdf => pdf.Name).ToList();

            var result = await controller.Put(null);

            Assert.IsInstanceOf<BadRequestObjectResult>(result);

            _mockStorage.Verify(s => s.ReOrder(It.IsAny<List<string>>()), Times.Never);
        }
        [Test]
        public void GivenStoreThrows_WhenReOrderCalled_ReturnReThrow()
        {
            PDFLibraryController controller = GetController();

            var reOrderList = GetListOf2PdfFileListItems().Select(pdf => pdf.Name).ToList();

            var thrown = new Exception();
            _mockStorage.Setup(s => s.ReOrder(reOrderList)).Throws(thrown);

            var ex = Assert.Throws<AggregateException>(() =>
            {
                var ret = controller.Put(reOrderList).Result;
            });

            Assert.AreEqual(thrown, ex.InnerException);
        }

        [Test]
        public async Task GivenPdfInStore_WhenDeleteCalled_ThenReturnDeleteThePdf()
        {
            PDFLibraryController controller = GetController();

            _mockStorage.Setup(s => s.CheckExists(TEST_FILENAME1)).
                Returns(Task.Factory.StartNew(() => true));

            var result = await controller.Delete(TEST_FILENAME1);

            Assert.IsInstanceOf<OkResult>(result);

            _mockStorage.Verify(s => s.CheckExists(TEST_FILENAME1), Times.Once);
            _mockStorage.Verify(s => s.Delete(TEST_FILENAME1), Times.Once);
        }

        [Test]
        public async Task GivenPdfNotInStore_WhenDeleteCalled_ThenReturnBadRequest()
        {
            PDFLibraryController controller = GetController();

            _mockStorage.Setup(s => s.CheckExists(TEST_FILENAME1)).
                Returns(Task.Factory.StartNew(() => false));

            var result = await controller.Delete(TEST_FILENAME1);

            Assert.IsInstanceOf<NotFoundResult>(result);

            _mockStorage.Verify(s => s.CheckExists(TEST_FILENAME1), Times.Once);
            _mockStorage.Verify(s => s.Delete(TEST_FILENAME1), Times.Never);
        }

        [Test]
        public void GivenStoreThrows_WhenDeleteCalled_ReturnReThrow()
        {
            PDFLibraryController controller = GetController();

            _mockStorage.Setup(s => s.CheckExists(TEST_FILENAME1)).
                Returns(Task.Factory.StartNew(() => true));

            var thrown = new Exception();
            _mockStorage.Setup(s => s.Delete(TEST_FILENAME1)).Throws(thrown);

            var ex = Assert.Throws<AggregateException>(() =>
            {
                var ret = controller.Delete(TEST_FILENAME1).Result;
            });

            Assert.AreEqual(thrown, ex.InnerException);
        }

        private static FormFile GetFormFile(PdfFile testPdfFile)
        {
            return new FormFile(testPdfFile.Content, 0, testPdfFile.Content.Length, testPdfFile.Name, testPdfFile.Name)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };
        }

        private static FormFile GetInvalidTypeFormFile()
        {
            return new FormFile(new MemoryStream(), 0, 5242881, null, "InvalidType.txt")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };
        }

        private static FormFile GetTooLargeFormFile()
        {
            return new FormFile(new MemoryStream(), 0, 5242881, null, "TooLarge.pdf")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };
        }

        private PDFLibraryController GetController()
        {
            PDFLibraryController controller = new PDFLibraryController(_mockStorage.Object, _mockLogger.Object);
            return controller;
        }
    }
}