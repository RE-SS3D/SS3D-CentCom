using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using CentCom.Dtos;
using CentCom.Helpers;
using CentCom.Interfaces;
using CentCom.Models;
using CentCom.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CentCom.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CharacterController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly CharacterService _characterService;
        private readonly IMapper _mapper;

        public CharacterController(UserManager<User> userManager, CharacterService characterService, IMapper mapper)
        {
            _userManager = userManager;
            _characterService = characterService;
            _mapper = mapper;
        }
        
        [HttpGet("all")]
        public IActionResult GetByUser()
        {
            long userId = Int64.Parse(_userManager.GetUserId(User));
            Character[] characters = _characterService.GetForUser(userId).ToArray();
            var charactersResponse = _mapper.Map<CharactersResponse>(characters);
            return Ok(charactersResponse);
        }
        
        [HttpPost("create")]
        public IActionResult Create([FromBody]CharacterRequest request)
        {
            var character = _mapper.Map<Character>(request);
            long userId = Int64.Parse(_userManager.GetUserId(User));
            
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
            long userId = Int64.Parse(_userManager.GetUserId(User));
            _characterService.Delete(userId, id);
            return Ok();
        }
    }
}