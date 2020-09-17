using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PDFLibrary.Api.Models;
using PDFLibrary.Api.Services;

namespace PDFLibrary.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PDFLibraryController : ControllerBase
    { 
        const int MAXPDFSIZE = 5242880;
        private readonly IPDFStoreBlobStorage _pdfStoreBlobStorage;
        private readonly ILogger<PDFLibraryController> _logger;

        public PDFLibraryController(IPDFStoreBlobStorage pdfStoreBlobStorage, ILogger<PDFLibraryController> logger)
        {
            _pdfStoreBlobStorage = pdfStoreBlobStorage;
            _logger = logger;
        }

        /// <summary>
        /// Get list of Pdf details
        /// </summary>
        /// <returns>List of Pdf details</returns>
        [HttpGet]
        public async Task<List<PdfFileListItem>> Get()
        {
            try
            {
                return await _pdfStoreBlobStorage.List();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception caught in Get()");
                throw;
            }     
        }

        /// <summary>
        /// Get (download) single Pdf
        /// </summary>
        /// <param name="fileName">Name of Pdf file</param>
        /// <returns>Pdf file</returns>
        [HttpGet("{fileName}")]
        public async Task<ActionResult> Get(string fileName)
        {
            try
            {
                //Validate param
                if (fileName == null)
                    return BadRequest("fileName parameter was null");

                //Validate exists
                if (! await _pdfStoreBlobStorage.CheckExists(fileName))
                    return NotFound();

                PdfFile pdfFile = await _pdfStoreBlobStorage.Download(fileName);
                return File(pdfFile.Content, pdfFile.ContentType, pdfFile.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception caught in Get(string fileName)");
                throw;
            }
        }

        /// <summary>
        /// Upload a Pdf
        /// </summary>
        /// <param name="file">Pdf file for storing</param>
        /// <returns>ActionResult (success/fail)</returns>
        [HttpPost]
        public async Task<ActionResult> Post([FromForm] IFormFile file)
        {
            try
            {
                //Validate param
                if (file == null)
                    return BadRequest("fileName parameter was null");

                //Validate size
                if (file.Length > MAXPDFSIZE)
                    return BadRequest($"The maximum file size is {MAXPDFSIZE} bytes");

                //Validate type
                //TBD validate content type
                if (Path.GetExtension(file.FileName)?.ToUpper() != ".PDF")
                    return BadRequest("The uploaded file must be a PDF");

                //Validate not exists
                if (await _pdfStoreBlobStorage.CheckExists(file.FileName))
                    return BadRequest("A file with a matching name already exists");

                await _pdfStoreBlobStorage.Add(
                    new PdfFile()
                    {
                        Name = file.FileName,
                        Content = file.OpenReadStream(),
                        ContentType = file.ContentType
                    });

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception caught in Post([FromForm] IFormFile file)");
                throw;
            }
        }

        /// <summary>
        /// Allows re-ordering of Pdf list
        /// </summary>
        /// <param name="newOrder">List of filenames representing new order</param>
        /// <returns>Success/fail</returns>
        [HttpPut]
        public async Task<ActionResult> Put([FromForm] List<string> newOrder)
        {
            try
            {
                //Validate param
                if (newOrder == null)
                    return BadRequest("newOrder parameter was null");

                //Validate all PDFs included
                await _pdfStoreBlobStorage.ReOrder(newOrder);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception caught in Put([FromForm] List<string> newOrder)");
                throw;
            }
        }

        /// <summary>
        /// Deletes a Pdf from store
        /// </summary>
        /// <param name="fileName">File name of Pdf to delete</param>
        /// <returns>Success/fail</returns>
        [HttpDelete("{fileName}")]
        public async Task<ActionResult> Delete(string fileName)
        {
            try
            {
                //Validate exists
                if (!await _pdfStoreBlobStorage.CheckExists(fileName))
                    return NotFound();
              
                await _pdfStoreBlobStorage.Delete(fileName);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception caught in Delete(string fileName)");
                throw;
            }
        }
    }
}
