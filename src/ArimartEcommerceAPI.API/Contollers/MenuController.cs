using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ArimartEcommerceAPI.Infrastructure.Data;
using ArimartEcommerceAPI.Infrastructure.Data.Models;
using System;

namespace ArimartEcommerceAPI.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MenuController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public MenuController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/Menu
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TblMenu>>> GetMenus()
    {
        return await _context.TblMenus
            .Where(m => m.IsDelete != true)
            .OrderBy(m => m.Position)
            .ToListAsync();
    }

    // GET: api/Menu/5
    [HttpGet("{id}")]
    public async Task<ActionResult<TblMenu>> GetMenu(long id)
    {
        var menu = await _context.TblMenus.FindAsync(id);

        if (menu == null || menu.IsDelete == true)
            return NotFound();

        return menu;
    }

    // POST: api/Menu
    [HttpPost]
    public async Task<ActionResult<TblMenu>> CreateMenu(TblMenu menu)
    {
        menu.CreateTime = DateTime.UtcNow;
        menu.IsDelete = false;
        _context.TblMenus.Add(menu);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMenu), new { id = menu.MenuId }, menu);
    }

    // PUT: api/Menu/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateMenu(long id, TblMenu menu)
    {
        if (id != menu.MenuId)
            return BadRequest();

        var existing = await _context.TblMenus.FindAsync(id);
        if (existing == null || existing.IsDelete == true)
            return NotFound();

        existing.MenuName = menu.MenuName;
        existing.MenuLink = menu.MenuLink;
        existing.Position = menu.Position;
        existing.IsActive = menu.IsActive;
        existing.IsRights = menu.IsRights;
        existing.IsAll = menu.IsAll;
        existing.IsCrm = menu.IsCrm;
        existing.ParentId = menu.ParentId;
        existing.ModifyTime = DateTime.UtcNow;

        _context.Entry(existing).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/Menu/5 (Soft Delete)
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMenu(long id)
    {
        var menu = await _context.TblMenus.FindAsync(id);
        if (menu == null || menu.IsDelete == true)
            return NotFound();

        menu.IsDelete = true;
        menu.ModifyTime = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
