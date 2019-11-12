﻿using HES.Core.Entities;
using HES.Core.Interfaces;
using HES.Core.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace HES.Core.Services
{
    public class TemplateService : ITemplateService
    {
        private readonly IAsyncRepository<Template> _templateRepository;

        public TemplateService(IAsyncRepository<Template> repository)
        {
            _templateRepository = repository;
        }

        public IQueryable<Template> Query()
        {
            return _templateRepository.Query();
        }

        public async Task<Template> GetByIdAsync(dynamic id)
        {
            return await _templateRepository.GetByIdAsync(id);
        }

        public async Task<List<Template>> GetTemplatesAsync()
        {
            return await _templateRepository.Query().ToListAsync();
        }

        public async Task<Template> CreateTmplateAsync(Template template)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            template.Urls = ValidationHepler.VerifyUrls(template.Urls);

            return await _templateRepository.AddAsync(template);
        }

        public async Task EditTemplateAsync(Template template)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            template.Urls = ValidationHepler.VerifyUrls(template.Urls);

            await _templateRepository.UpdateAsync(template);
        }

        public async Task DeleteTemplateAsync(string id)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }
            var template = await GetByIdAsync(id);
            if (template == null)
            {
                throw new Exception("Template does not exist.");
            }
            await _templateRepository.DeleteAsync(template);
        }

        public async Task<bool> ExistAsync(Expression<Func<Template, bool>> predicate)
        {
            return await _templateRepository.ExistAsync(predicate);
        }
    }
}