﻿using JobPortal.Data;
using JobPortal.Helper;
using JobPortal.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Services
{
    public class CompanyService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly JobPortalContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CompanyService(JobPortalContext context, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _webHostEnvironment = webHostEnvironment;
        }

        // Register a company
        public async Task<Company> RegisterCompanyAsync(Company company,IFormFile image, string password)
        {
            // Create ApplicationUser and set properties
            var user = new ApplicationUser
            {
                Id = company.CompanyId.ToString(),
                UserName = company.Email,
                Email = company.Email,
                // Additional properties if required
            };

            string path = await ImageHelper.SaveImageAsync(image, _webHostEnvironment.WebRootPath, "Upload/CompanyImage", user.Id);

            user.ProfileImageUrl = path;

            // Create the user in the identity system
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                // Handle errors
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            // Assign the "Company" role
            await _userManager.AddToRoleAsync(user, "Company");

            // Save the company to the database
            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            return company;
        }


        // Authenticate a company
        public async Task<ApplicationUser?> AuthenticateCompanyAsync(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return null;

            var result = await _signInManager.PasswordSignInAsync(user, password, false, false);

            if (result.Succeeded)
            {
                return user;
            }
            return null;
        }
        public async Task<List<Company>> GetCompanies()
        {
            return await _context.Companies.ToListAsync();
        }

        public async Task<Company?> GetCompanyById(Guid companyId)
        {
            return await _context.Companies.Include(c => c.Jobs).FirstOrDefaultAsync(c => c.CompanyId == companyId);
        }

        public async Task<List<Company>> GetCompaniesByIndustry(Guid industryId)
        {
            return await _context.Companies.Where(c => c.IndustryId == industryId).ToListAsync();
        }

        public async Task<List<Job>> GetJobsByCompany(Guid companyId)
        {
            var company = await _context.Companies.Include(c => c.Jobs).FirstOrDefaultAsync(c => c.CompanyId == companyId);
            return company?.Jobs.ToList() ?? new List<Job>();
        }

        public async Task<List<Company>> SearchCompanies(string name)
        {
            return await _context.Companies.Where(c => c.Name.Contains(name)).ToListAsync();
        }

        public async Task<Job> PostJob(Guid companyId, Job job)
        {
            job.CompanyId = companyId;
            job.PostedDate = DateTime.UtcNow;
            _context.Jobs.Add(job);
            await _context.SaveChangesAsync();
            return job;
        }
    }

}
