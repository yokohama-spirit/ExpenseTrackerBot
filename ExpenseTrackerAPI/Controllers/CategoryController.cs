using ExpenseTrackerLibrary.Application.Dto;
using ExpenseTrackerLibrary.Domain.Entities;
using ExpenseTrackerLibrary.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTrackerAPI.Controllers
{
    [Route("api/category")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryRepository _rep;

        public CategoryController(ICategoryRepository rep)
        {
            _rep = rep;
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDTO command)
        {
            try
            {
                await _rep.CreateCategory(command.Name, command.ChatId);
                return Ok("Категория успешно создана!");
            }
            catch (Exception ex)
            {
                return BadRequest($"Ошибка: {ex}");
            }
        }


        [HttpGet("checkm/{name}/{chatId}")]
        public async Task<ActionResult<decimal>> CheckMonthlyByCategory(string name, long chatId)
        {
            var result = await _rep.CheckMonthlyByCategory(name, chatId);
            return result;
        }

        [HttpGet("checkw/{name}/{chatId}")]
        public async Task<ActionResult<decimal>> CheckWeeklyByCategory(string name, long chatId)
        {
            var result = await _rep.CheckWeeklyByCategory(name, chatId);
            return result;
        }

        [HttpGet("mycat/{chatId}")]
        public async Task<ActionResult<string>> GetMyCategories(long chatId)
        {
            var result = await _rep.GetUserCategoriesString(chatId);
            return Ok(result);
        }

        [HttpGet("ix/{name}/{chatId}")]
        public async Task<ActionResult<bool>> IsExistsCategory(string name, long chatId)
        {
            var result = await _rep.isExistsCategory(name, chatId);
            return result;
        }
    }
}
