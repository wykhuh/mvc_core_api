﻿using AutoMapper;
using CodeCamp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyCodeCamp.Data;
using MyCodeCamp.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CodeCamp.Controllers
{
    // base route for all the actions in the controler
    [Route("api/[controller]")]
    public class CampsController : Controller
    {
        private ICampRepository _repo;
        private ILogger _logger;
        private IMapper _mapper;

        // constructor inject to access dependencies
        public CampsController(ICampRepository repo, 
            ILogger<CampsController> logger,
            IMapper mapper)
        {
            // save copy of passed-in repo
            _repo = repo;
            _logger = logger;
            _mapper = mapper;
        }
        [HttpGet("")]
        // use IActionResult to return status code with the data 
        public IActionResult Get()
        {
            var camps = _repo.GetAllCamps();

            return Ok(_mapper.Map<IEnumerable<CampModel>>(camps));
        }

        [HttpGet("{id}", Name ="CampGet")]
        // MVC assumes any pass in parameter listed in the url are query string parameters
        public IActionResult Get(int id, bool includeSpeakers = false)
        {
            try
            {
                Camp camp = null;

                if (includeSpeakers) camp = _repo.GetCampWithSpeakers(id);
                else camp = _repo.GetCamp(id);

                if (camp == null) return NotFound($"Camp {id} not found.");

                return Ok(_mapper.Map<CampModel>(camp));
            }
            catch
            {
                return BadRequest();
            }

        }

        [HttpPost("")]
        // use [FromBody] if incoming data is json
        // make request async using async Task<> ... await
        public async Task<IActionResult> Post([FromBody]Camp model)
        {
            try
            {
                _logger.LogInformation("Creating new camp.");

                // save model to server;
                // the return model will have server generated data such as id
                _repo.Add(model);
                if (await _repo.SaveAllAsync())
                {
                    // pass in id to Url.Link via anonymous object
                    var newUri = Url.Link("CampGet", new { id = model.Id });

                    // use Created status for Post.
                    // Created needs new uri and record created.
                    // model will contain server generated data.
                    return Created(newUri, model);
                }
                else
                {
                    _logger.LogWarning("Could not save camp.");
                }
            }
            catch(Exception ex)
            {
                _logger.LogError($"Save camp exception: {ex}");
            }
            return BadRequest();
        }

        // accept both PUT and PATCH
        [HttpPut("{id}")]
        [HttpPatch("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Camp model)
        {
            try
            {
                var oldCamp = _repo.GetCamp(id);
                if (oldCamp == null) return NotFound($"Could not find camp id {id}");

                // map model to oldCamp
                // if model.Name is null, assign oldCamp.Name
                oldCamp.Name = model.Name ?? oldCamp.Name;
                oldCamp.Description = model.Description ?? oldCamp.Description;
                oldCamp.Location = model.Location ?? oldCamp.Location;
                oldCamp.Length = model.Length > 0 ? model.Length : oldCamp.Length;
                oldCamp.EventDate = model.EventDate != DateTime.MinValue ? model.EventDate : oldCamp.EventDate;


                if (await _repo.SaveAllAsync())
                {
                    // oldCamp has been updated with changes
                    return Ok(oldCamp);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"update camp exception: {ex}");
            }
            return BadRequest();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var oldCamp = _repo.GetCamp(id);
                if (oldCamp == null) return NotFound($"Could not find camp id {id}");

                // pass in whole camp instead of just id so we can examine the camp before deleting
                _repo.Delete(oldCamp);

                if (await _repo.SaveAllAsync())
                {
                    return Ok();
                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"delete camp exception: {ex}");
            }
            return BadRequest();
        }

    }
}
