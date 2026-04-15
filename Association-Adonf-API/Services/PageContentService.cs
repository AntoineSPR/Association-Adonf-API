using AssociationAdonfAPI.Context;
using AssociationAdonfAPI.Models;
using AssociationAdonfAPI.Models.PageContentDTOs;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AssociationAdonfAPI.Services
{
    public class PageContentService
    {
        private readonly DataContext _context;

        public PageContentService(DataContext context)
        {
            _context = context;
        }

        public async Task<PageContentDto?> GetBySlugAsync(string slug)
        {
            var cleanSlug = slug.ToLower().Trim();
            var content = await _context.PageContents
                .Where(p => p.Slug == cleanSlug && !p.IsArchived)
                .FirstOrDefaultAsync();

            if (content == null) return null;

            // --- HYDRATATION STRICTE PAR ID ---
            if (cleanSlug != "contacts" && content.Content != null)
            {
                try
                {
                    var pageContentRaw = content.Content.RootElement.GetRawText();
                    if (!string.IsNullOrEmpty(pageContentRaw))
                    {
                        var pageRoot = JsonNode.Parse(pageContentRaw);
                        
                        var contactsPage = await _context.PageContents
                            .Where(p => p.Slug == "contacts" && !p.IsArchived)
                            .FirstOrDefaultAsync();

                        if (contactsPage?.Content != null)
                        {
                            var globalRoot = JsonNode.Parse(contactsPage.Content.RootElement.GetRawText());
                            var globalContactsArr = globalRoot?["contacts"] as JsonArray;
                            var globalContactsById = new System.Collections.Generic.Dictionary<string, JsonNode>();

                            if (globalContactsArr != null)
                            {
                                foreach (var gc in globalContactsArr)
                                {
                                    var id = gc?["id"]?.ToString();
                                    if (!string.IsNullOrEmpty(id)) globalContactsById[id] = gc;
                                }
                            }

                            bool modified = false;
                            HydrateContactsRecursively(pageRoot, globalContactsById, ref modified);

                            if (modified)
                            {
                                return new PageContentDto
                                {
                                    Id = content.Id,
                                    Slug = content.Slug,
                                    Content = JsonDocument.Parse(pageRoot!.ToJsonString())
                                };
                            }
                        }
                    }
                }
                catch { /* Ignorer exception d'hydratation */ }
            }

            return new PageContentDto
            {
                Id = content.Id,
                Slug = content.Slug,
                Content = content.Content
            };
        }

        public async Task<PageContentDto> CreateOrUpdateAsync(string slug, PageContentUpdateDto updateDto)
        {
            var cleanSlug = slug.ToLower().Trim();

            // Création des IDs si la page contacts est modifiée
            if (cleanSlug == "contacts" && updateDto.Content != null)
            {
                try
                {
                    var rootNode = JsonNode.Parse(updateDto.Content.RootElement.GetRawText()) as JsonObject;
                    if (rootNode != null && rootNode.ContainsKey("contacts") && rootNode["contacts"] is JsonArray contactsArr)
                    {
                        bool modified = false;
                        foreach (var c in contactsArr)
                        {
                            if (c is JsonObject cObj)
                            {
                                if (!cObj.ContainsKey("id") || string.IsNullOrEmpty(cObj["id"]?.ToString()))
                                {
                                    cObj["id"] = System.Guid.NewGuid().ToString();
                                    modified = true;
                                }
                            }
                        }
                        if (modified) updateDto.Content = JsonDocument.Parse(rootNode.ToJsonString());
                    }
                }
                catch { }
            }
            // Déshydratation (on ne sauvegarde QUE les IDs) pour les autres pages
            else if (cleanSlug != "contacts" && updateDto.Content != null)
            {
                try
                {
                    var rootNode = JsonNode.Parse(updateDto.Content.RootElement.GetRawText());
                    bool modified = false;
                    DehydrateContactsRecursively(rootNode, ref modified);
                    if (modified) updateDto.Content = JsonDocument.Parse(rootNode!.ToJsonString());
                }
                catch { }
            }

            var existing = await _context.PageContents
                .Where(p => p.Slug == cleanSlug && !p.IsArchived)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                existing.Content = updateDto.Content;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                existing = new PageContent
                {
                    Slug = cleanSlug,
                    Content = updateDto.Content
                };
                _context.PageContents.Add(existing);
            }

            await _context.SaveChangesAsync();

            return new PageContentDto
            {
                Id = existing.Id,
                Slug = existing.Slug,
                Content = existing.Content
            };
        }

        private void HydrateContactsRecursively(JsonNode? node, System.Collections.Generic.Dictionary<string, JsonNode> globalById, ref bool modified)
        {
            if (node is JsonObject obj)
            {
                if (obj.ContainsKey("contacts") && obj["contacts"] is JsonArray contactsArr)
                {
                    var newContacts = new JsonArray();
                    foreach (var c in contactsArr)
                    {
                        if (c is JsonObject cObj)
                        {
                            string? contactId = cObj["contactId"]?.ToString();
                            if (!string.IsNullOrEmpty(contactId) && globalById.ContainsKey(contactId))
                            {
                                var fresh = JsonNode.Parse(globalById[contactId]!.ToJsonString()) as JsonObject;
                                fresh!["contactId"] = contactId; 
                                newContacts.Add(fresh);
                            }
                        }
                    }

                    if (contactsArr.ToJsonString() != newContacts.ToJsonString())
                    {
                        obj["contacts"] = newContacts;
                        modified = true;
                    }
                }

                foreach (var kvp in obj.ToArray())
                {
                    if (kvp.Key != "contacts") HydrateContactsRecursively(kvp.Value, globalById, ref modified);
                }
            }
            else if (node is JsonArray arr)
            {
                foreach (var item in arr) HydrateContactsRecursively(item, globalById, ref modified);
            }
        }

        private void DehydrateContactsRecursively(JsonNode? node, ref bool modified)
        {
            if (node is JsonObject obj)
            {
                if (obj.ContainsKey("contacts") && obj["contacts"] is JsonArray contactsArr)
                {
                    var newContacts = new JsonArray();
                    foreach (var c in contactsArr)
                    {
                        if (c is JsonObject cObj)
                        {
                            string? contactId = cObj["contactId"]?.ToString();
                            if (!string.IsNullOrEmpty(contactId))
                            {
                                newContacts.Add(new JsonObject { ["contactId"] = contactId });
                            }
                        }
                    }
                    if (contactsArr.ToJsonString() != newContacts.ToJsonString())
                    {
                        obj["contacts"] = newContacts;
                        modified = true;
                    }
                }

                foreach (var kvp in obj.ToArray())
                {
                    if (kvp.Key != "contacts") DehydrateContactsRecursively(kvp.Value, ref modified);
                }
            }
            else if (node is JsonArray arr)
            {
                foreach (var item in arr) DehydrateContactsRecursively(item, ref modified);
            }
        }
    }
}
