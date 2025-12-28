using Microsoft.AspNetCore.Mvc;
using MonitoringSystem.Data;
using MonitoringSystem.Models;
using System.Linq;
using System.Threading.Tasks;

namespace MonitoringSystem.Controllers
{
    public class CompanyController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CompanyController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================== INDEX ==================
        // GET: Company
        public IActionResult Index()
        {
            var companies = _context.Companies.ToList();
            return View(companies);
        }

        // ================== CREATE ==================
        // GET: Company/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Company/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Company company)
        {
            if (ModelState.IsValid)
            {
                _context.Companies.Add(company);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(company);
        }

        // ================== EDIT ==================
        // GET: Company/Edit/5
        public IActionResult Edit(int? id)
        {
            if (id == null) return NotFound();

            var company = _context.Companies.Find(id);
            if (company == null) return NotFound();

            return View(company);
        }

        // POST: Company/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Company company)
        {
            if (id != company.Id) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(company);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(company);
        }

        // ================== DELETE ==================
        // GET: Company/Delete/5
        public IActionResult Delete(int? id)
        {
            if (id == null) return NotFound();

            var company = _context.Companies.Find(id);
            if (company == null) return NotFound();

            return View(company);
        }

        // POST: Company/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var company = _context.Companies.Find(id);
            if (company != null)
            {
                _context.Companies.Remove(company);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
