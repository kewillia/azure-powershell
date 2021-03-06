﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Commands.Tags.Properties;
using Microsoft.Azure.Management.Resources;
using Microsoft.Azure.Management.Resources.Models;
using Microsoft.WindowsAzure.Commands.Common;
using Microsoft.Azure.Common.Authentication.Models;
using Microsoft.WindowsAzure.Commands.Utilities.Common;
using Microsoft.Azure.Common.Authentication;

namespace Microsoft.Azure.Commands.Tags.Model
{
    public class TagsClient
    {
        public const string ExecludedTagPrefix = "hidden-related:/";

        public IResourceManagementClient ResourceManagementClient { get; set; }

        public Action<string> VerboseLogger { get; set; }

        public Action<string> ErrorLogger { get; set; }

        /// <summary>
        /// Creates new TagsClient
        /// </summary>
        /// <param name="subscription">Subscription containing resources to manipulate</param>
        public TagsClient(AzureProfile profile, AzureSubscription subscription)
            : this(AzureSession.ClientFactory.CreateClient<ResourceManagementClient>(profile, subscription, AzureEnvironment.Endpoint.ResourceManager))
        {

        }

        /// <summary>
        /// Creates new TagsClient instance
        /// </summary>
        /// <param name="resourceManagementClient">The IResourceManagementClient instance</param>
        public TagsClient(IResourceManagementClient resourceManagementClient)
        {
            ResourceManagementClient = resourceManagementClient;
        }

        /// <summary>
        /// Parameterless constructor for mocking
        /// </summary>
        public TagsClient()
        {

        }

        public List<PSTag> ListTags()
        {
            TagsListResult result = ResourceManagementClient.Tags.List();
            List<PSTag> tags = new List<PSTag>();

            do
            {
                result.Tags.Where(t => !t.Name.StartsWith(ExecludedTagPrefix)).ForEach(t => tags.Add(t.ToPSTag()));

                if (!string.IsNullOrEmpty(result.NextLink))
                {
                    result = ResourceManagementClient.Tags.ListNext(result.NextLink);
                }
            } while (!string.IsNullOrEmpty(result.NextLink));

            return tags;
        }

        public PSTag GetTag(string tag)
        {
            List<PSTag> tags = ListTags();
            if (!tags.Exists(t => t.Name.Equals(tag, StringComparison.OrdinalIgnoreCase)))
            {
                throw new Exception(string.Format(Resources.TagNotFoundMessage, tag));
            }

            return tags.First(t => t.Name.Equals(tag, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Creates a tag and if the tag name exists add the value to the existing tag name.
        /// </summary>
        /// <param name="tag">The tag name</param>
        /// <param name="values">The tag values</param>
        /// <returns>The tag object</returns>
        public PSTag CreateTag(string tag, List<string> values)
        {
            ResourceManagementClient.Tags.CreateOrUpdate(tag);

            if (values != null)
            {
                values.ForEach(v => ResourceManagementClient.Tags.CreateOrUpdateValue(tag, v));
            }

            return GetTag(tag);
        }

        /// <summary>
        /// Deletes the entire tag or specific tag value.
        /// </summary>
        /// <param name="tag">The tag name</param>
        /// <param name="values">Values to remove</param>
        /// <returns></returns>
        public PSTag DeleteTag(string tag, List<string> values)
        {
            PSTag tagObject = null;


            if (values == null || values.Count != 1)
            {
                tagObject = GetTag(tag);
                if (int.Parse(tagObject.Count) > 0)
                {
                    throw new Exception(Resources.CanNotDeleteTag);
                }
            }

            if (values == null || values.Count == 0)
            {
                tagObject = GetTag(tag);
                tagObject.Values.ForEach(v => ResourceManagementClient.Tags.DeleteValue(tag, v.Name));
                ResourceManagementClient.Tags.Delete(tag);
            }
            else
            {
                values.ForEach(v => ResourceManagementClient.Tags.DeleteValue(tag, v));
                tagObject = GetTag(tag);
            }

            return tagObject;
        }
    }
}
