using System;
using System.Linq;
using Api.Dtos;
using Api.Helpers;
using Api.Interfaces;
using Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CharacterController : ControllerBase
    {
        private ICharacterService _characterService;

        public CharacterController(ICharacterService characterService)
        {
            _characterService = characterService;
        }
        
        [HttpGet("all")]
        public IActionResult GetByUser()
        {
            long id = Int64.Parse(User.Identity.Name);
            Character[] characters = _characterService.GetForUser(id).ToArray();
            var charactersResponse = new CharactersResponse(characters);
            return Ok(charactersResponse);
        }
        
        [HttpPost("create")]
        public IActionResult Create([FromBody]CharacterRequest request)
        {
            var character = Character.From(request);
            long userId = Int64.Parse(User.Identity.Name);
            
            try 
            {
                _characterService.Create(character, userId);
                return Ok();
            } 
            catch(AppException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            try 
            {
                long userId = Int64.Parse(User.Identity.Name);
                _characterService.Delete(userId, id);
                return Ok();
            } 
            catch(AppException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}