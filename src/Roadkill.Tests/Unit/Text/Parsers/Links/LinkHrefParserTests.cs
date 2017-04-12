﻿using System;
using NUnit.Framework;
using Roadkill.Core.Configuration;
using Roadkill.Core.Database;
using Roadkill.Core.Text.Parsers.Links;
using Roadkill.Tests.Unit.StubsAndMocks;

namespace Roadkill.Tests.Unit.Text.Parsers.Links
{
	public class LinkHrefParserTests
	{
		// Many of these tests were converted from the v1.7 MarkdownConverter tests.

		private PageRepositoryMock _pageRepository;
		private ApplicationSettings _applicationSettings;
		private LinkHrefParser _linkHrefParser;
		private UrlHelperMock _urlHelper;

		[SetUp]
		public void Setup()
		{
			var container = new MocksAndStubsContainer();
			_pageRepository = container.PageRepository;
			_applicationSettings = container.ApplicationSettings;
			_urlHelper = new UrlHelperMock();
			//_urlResolver.InternalUrlFormat = ;
			
			_linkHrefParser = new LinkHrefParser(_pageRepository, _applicationSettings, _urlHelper);
		}

		[Test]
		[TestCase("http://www.example.com")]
		[TestCase("https://www.example.com")]
		[TestCase("www.example.com")]
		[TestCase("mailto:me@example.com")]
		[TestCase("tag:the-architecture-of-old")]
		public void should_add_external_links_css_class_to_links_and_keep_url(string url)
		{
			// Arrange
			HtmlLinkTag linkTag = new HtmlLinkTag(url, url, "text", "");

			// Act
			HtmlLinkTag actualTag = _linkHrefParser.Parse(linkTag);

			// Assert
			Assert.That(actualTag.Href, Is.EqualTo(url));
			Assert.That(actualTag.CssClass, Is.EqualTo("external-link"));
		}

		[Test]
		public void should_not_add_external_link_cssclass_for_anchor_tags()
		{
			// Arrange
			HtmlLinkTag linkTag = new HtmlLinkTag("#my-anchor", "#my-anchor", "text", "");

			// Act
			HtmlLinkTag actualTag = _linkHrefParser.Parse(linkTag);

			// Assert
			Assert.That(actualTag.Href, Is.EqualTo("#my-anchor"));
			Assert.That(actualTag.CssClass, Is.EqualTo(""));
		}

		[Test]
		public void should_add_missing_page_link_css_class_when_internal_link_does_not_exist()
		{
			// Arrange
			HtmlLinkTag linkTag = new HtmlLinkTag("doesnt-exist", "doesnt-exist", "text", "");

			// Act
			HtmlLinkTag actualTag = _linkHrefParser.Parse(linkTag);

			// Assert
			Assert.That(actualTag.CssClass, Is.EqualTo("missing-page-link"));
		}

		[Test]
		public void should_match_link_with_dashes_with_page_in_repository()
		{
			// Arrange
			_pageRepository.AddNewPage(new Page() { Id = 1, Title = "my page on engineering" }, "text", "admin", DateTime.Today);
			HtmlLinkTag linkTag = new HtmlLinkTag("my-page-on-engineering", "my-page-on-engineering", "text", "");

			// Act
			HtmlLinkTag actualTag = _linkHrefParser.Parse(linkTag);

			// Assert
			Assert.That(actualTag.OriginalHref, Is.EqualTo("my-page-on-engineering"));
			Assert.That(actualTag.Href, Is.EqualTo("/wiki/1/my-page-on-engineering"));
		}

		[Test]
		public void should_use_url_resolver_for_special_pages()
		{
			// Arrange
			HtmlLinkTag linkTag = new HtmlLinkTag("Special:blah", "Special:blah", "text", "");

			// Act
			HtmlLinkTag actualTag = _linkHrefParser.Parse(linkTag);

			// Assert
			Assert.That(actualTag.Href, Is.EqualTo("~/wiki/Special:blah/this-is-here-if-resolver-was-called"));
		}

		[Test]
		public void href_with_dashes_and_23_are_not_encoded()
		{
			// Arrange
			HtmlLinkTag linkTag = new HtmlLinkTag("https://www.google.com/some-page-23", "https://www.google.com/some-page-23", "text", "");

			// Act
			HtmlLinkTag actualTag = _linkHrefParser.Parse(linkTag);

			// Assert
			Assert.That(actualTag.Href, Is.EqualTo("https://www.google.com/some-page-23"));
		}

		[Test]
		public void href_with_tilde_should_resolve_as_attachment_paths()
		{
			// Arrange
			HtmlLinkTag linkTag = new HtmlLinkTag("~/my/folder/image1.jpg", "~/my/folder/image1.jpg", "text", "");

			// Act
			HtmlLinkTag actualTag = _linkHrefParser.Parse(linkTag);

			// Assert
			Assert.That(actualTag.Href, Is.EqualTo("/Attachments/my/folder/image1.jpg"));
		}

		[Test]
		public void href_external_link_with_anchor_should_retain_anchor()
		{
			// Arrange - Issue #172 (Bitbucket)
			HtmlLinkTag linkTag = new HtmlLinkTag("http://www.google.com/?blah=xyz#myanchor", "http://www.google.com/?blah=xyz#myanchor", "text", "");

			// Act
			HtmlLinkTag actualTag = _linkHrefParser.Parse(linkTag);

			// Assert
			Assert.That(actualTag.Href, Is.EqualTo("http://www.google.com/?blah=xyz#myanchor"));
		}

		[Test]
		public void href_external_link_with_urlencoded_anchor_should_retain_anchor()
		{
			// Arrange - Issue #172 (Bitbucket)
			HtmlLinkTag linkTag = new HtmlLinkTag("http://www.google.com/?blah=xyz%23myanchor", "http://www.google.com/?blah=xyz%23myanchor", "text", "");

			// Act
			HtmlLinkTag actualTag = _linkHrefParser.Parse(linkTag);

			// Assert
			Assert.That(actualTag.Href, Is.EqualTo("http://www.google.com/?blah=xyz%23myanchor"));
		}

		[Test]
		public void href_internal_links_with_anchor_tag_should_retain_anchor()
		{
			// Arrange
			_pageRepository.AddNewPage(new Page() { Id = 1, Title = "foo" }, "text", "admin", DateTime.Today);
			HtmlLinkTag linkTag = new HtmlLinkTag("foo#myanchor", "foo#myanchor", "text", "");

			// Act
			HtmlLinkTag actualTag = _linkHrefParser.Parse(linkTag);

			// Assert
			Assert.That(actualTag.Href, Is.EqualTo("/wiki/1/foo#myanchor"));
		}

		[Test]
		public void pages_with_dash_in_title()
		{
			// Arrange
			_pageRepository.AddNewPage(new Page() { Id = 1, Title = "foo page" }, "text", "admin", DateTime.Today);
			HtmlLinkTag linkTag = new HtmlLinkTag("foo-page?blah=xyz#myanchor", "foo-page?blah=xyz#myanchor", "text", "");

			// Act
			HtmlLinkTag actualTag = _linkHrefParser.Parse(linkTag);

			// Assert
			Assert.Fail("fail");
		}

		[Test]
		public void href_internal_links_with_querystring_and_anchor_tag_should_find_page_and_retain_querystring_and_anchor()
		{
			// Arrange
			_pageRepository.AddNewPage(new Page() { Id = 1, Title = "trump" }, "text", "admin", DateTime.Today);
			HtmlLinkTag linkTag = new HtmlLinkTag("foo-page?blah=xyz#myanchor", "foo-page?blah=xyz#myanchor", "text", "");

			// Act
			HtmlLinkTag actualTag = _linkHrefParser.Parse(linkTag);

			// Assert
			Assert.That(actualTag.Href, Is.EqualTo("/wiki/1/trump?blah=xyz#myanchor"));
		}

		[Test]
		public void href_internal_existing_wiki_page_link_should_return_href_with_wiki_prefix()
		{
			// Arrange
			_pageRepository.AddNewPage(new Page() { Id = 1, Title = "football" }, "text", "admin", DateTime.Today);
			HtmlLinkTag linkTag = new HtmlLinkTag("football", "foo-page", "text", "");

			// Act
			HtmlLinkTag actualTag = _linkHrefParser.Parse(linkTag);

			// Assert
			Assert.That(actualTag.Href, Is.EqualTo("/wiki/1/football"));
		}

		[Test]
		public void href_links_with_the_word_script_in_url_should_not_be_cleaned()
		{
			// Arrange - Issue #159 (Bitbucket) (deSCRIPTion)
			HtmlLinkTag linkTag = new HtmlLinkTag("http://msdn.microsoft.com/en-us/library/system.componentmodel.descriptionattribute.aspx", "http://msdn.microsoft.com/en-us/library/system.componentmodel.descriptionattribute.aspx", "Component description", "");

			// Act
			HtmlLinkTag actualTag = _linkHrefParser.Parse(linkTag);

			// Assert
			Assert.That(actualTag.Href, Is.EqualTo("http://msdn.microsoft.com/en-us/library/system.componentmodel.descriptionattribute.aspx"));
		}

		[Test]
		[TestCase("attachment:/")]
		[TestCase("~/")]
		public void lhref_inks_starting_with_attachments_should_resolve_as_attachment_paths(string prefix)
		{
			// Arrange
			string actualPath = $"{prefix}my/folder/image1.jpg";
			HtmlLinkTag linkTag = new HtmlLinkTag(actualPath, actualPath, "text", "");

			// Act
			HtmlLinkTag actualTag = _linkHrefParser.Parse(linkTag);

			// Assert
			Assert.That(actualTag.Href, Is.EqualTo("/Attachments/my/folder/image1.jpg"));
		}

		[Test]
		public void links_starting_with_special_should_resolve_as_full_specialpage()
		{
			// Arrange
			HtmlLinkTag linkTag = new HtmlLinkTag("Special:Foo", "Special:Foo", "text", "");

			// Act
			HtmlLinkTag actualTag = _linkHrefParser.Parse(linkTag);

			// Assert
			Assert.That(actualTag.Href, Is.EqualTo("~/wiki/Special:Foo")); // note: "~" is replaced by ASP.NET HttpContext
		}

		[Test]
		public void href_links_starting_with_mailto_tag_are_not_rewritten_as_internal_and_have_external_link_css_class()
		{
			// Arrange
			HtmlLinkTag linkTag = new HtmlLinkTag("mailto:spam@gmail.com", "mailto:spam@gmail.com", "text", "");

			// Act
			HtmlLinkTag actualTag = _linkHrefParser.Parse(linkTag);

			// Assert
			Assert.That(actualTag.Href, Is.EqualTo("mailto:spam@gmail.com"));
			Assert.That(actualTag.CssClass, Is.EqualTo("external-link"));
		}

		[Test]
		public void newpage()
		{
			// Arrange
			HtmlLinkTag linkTag = new HtmlLinkTag("newpage", "newpage", "text", "");

			// Act
			HtmlLinkTag actualTag = _linkHrefParser.Parse(linkTag);

			// Assert
			Assert.That(actualTag.Href, Is.EqualTo("mailto:spam@gmail.com"));
			Assert.That(actualTag.CssClass, Is.EqualTo("external-link"));
		}
	}
}