using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlogCore.DAL.Tests
{
    [TestClass]
    public class BlogRepositoryTests : IntegrationTestBase
    {
        [TestMethod]
        public void AddPost_ShouldIncreasePostCount()
        {
            var post = DataGenerator.GetPostFaker().Generate();
            var initialCount = _context.Posts.Count();

            _repository.AddPost(post);
            var finalCount = _context.Posts.Count();

            Assert.AreEqual(initialCount + 1, finalCount);
        }

        [TestMethod]
        [ExpectedException(typeof(Microsoft.EntityFrameworkCore.DbUpdateException))]
        public void AddPost_WithNullAuthor_ShouldThrowDbUpdateException()
        {
            var post = DataGenerator.GetPostFaker().Generate();
            post.Author = null!; 

            _repository.AddPost(post);
        }

        [TestMethod]
        public void GetCommentsByPostId_ShouldReturnThreeComments()
        {
            var post = DataGenerator.GetPostFaker().Generate();
            _repository.AddPost(post);

            var comments = DataGenerator
                .GetCommentFaker(post.Id)
                .Generate(3);

            foreach (var comment in comments)
            {
                _repository.AddComment(comment);
            }

            var result = _repository.GetCommentsByPostId(post.Id);

            foreach (var addedComment in comments)
            {
                Assert.IsTrue(result.Any(r =>
                    r.Id == addedComment.Id &&
                    r.Content == addedComment.Content &&
                    r.PostId == addedComment.PostId));
            }
        }

        [TestMethod]
        public void GetAllPosts_EmptyDb_ReturnsZero()
        {
            var posts = _repository.GetAllPosts();

            Assert.AreEqual(0, posts.Count());
        }

        [TestMethod]
        public void AddPost_LongContent_SavesCorrectly()
        {
            var faker = DataGenerator.GetPostFaker();

            var post = faker.Generate();
            post.Content = new Bogus.Faker().Lorem.Paragraphs(5);

            _repository.AddPost(post);

            var savedPost = _context.Posts.First();

            Assert.AreEqual(post.Content, savedPost.Content);
        }

        [TestMethod]
        public void AddPost_SpecialCharactersInAuthor_SavesCorrectly()
        {
            var post = DataGenerator.GetPostFaker().Generate();
            post.Author = "Zażółć Gęślą Jaźń 123!";

            _repository.AddPost(post);

            var savedPost = _context.Posts.First();

            Assert.AreEqual("Zażółć Gęślą Jaźń 123!", savedPost.Author);
        }

        [TestMethod]
        public void AddComment_ValidData_IncreasesCountForPost()
        {
            var post = DataGenerator.GetPostFaker().Generate();
            _repository.AddPost(post);

            var comment = DataGenerator.GetCommentFaker(post.Id).Generate();

            _repository.AddComment(comment);
            var comments = _repository.GetCommentsByPostId(post.Id);

            Assert.AreEqual(1, comments.Count());
        }

        [TestMethod]
        public void GetCommentsByPostId_NonExistentPost_ReturnsEmpty()
        {
            int nonExistingPostId = 9999;

            var comments = _repository.GetCommentsByPostId(nonExistingPostId);

            Assert.IsNotNull(comments);
            Assert.AreEqual(0, comments.Count());
        }

        [TestMethod]
        [ExpectedException(typeof(Microsoft.EntityFrameworkCore.DbUpdateException))]
        public void AddComment_OrphanComment_ThrowsException()
        {
            int nonExistingPostId = 9999;

            var comment = DataGenerator
                .GetCommentFaker(nonExistingPostId)
                .Generate();

            _repository.AddComment(comment);
        }

        [TestMethod]
        public void MultipleComments_DifferentPosts_ReturnsOnlyCorrectOnes()
        {
            var post1 = DataGenerator.GetPostFaker().Generate();
            var post2 = DataGenerator.GetPostFaker().Generate();

            _repository.AddPost(post1);
            _repository.AddPost(post2);

            var commentsPost1 = DataGenerator.GetCommentFaker(post1.Id).Generate(5);
            var commentsPost2 = DataGenerator.GetCommentFaker(post2.Id).Generate(2);

            foreach (var c in commentsPost1)
                _repository.AddComment(c);

            foreach (var c in commentsPost2)
                _repository.AddComment(c);

            var result = _repository.GetCommentsByPostId(post1.Id).ToList();

            Assert.AreEqual(5, result.Count);

            foreach (var comment in result)
            {
                Assert.AreEqual(post1.Id, comment.PostId);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(Microsoft.EntityFrameworkCore.DbUpdateException))]
        public void AddPost_NullAuthor_ThrowsDbUpdateException()
        {
            var post = DataGenerator.GetPostFaker().Generate();
            post.Author = null!;

            _repository.AddPost(post);
        }

        [TestMethod]
        [ExpectedException(typeof(Microsoft.EntityFrameworkCore.DbUpdateException))]
        public void AddComment_NullContent_ThrowsDbUpdateException()
        {
            var post = DataGenerator.GetPostFaker().Generate();
            _repository.AddPost(post);

            var comment = DataGenerator.GetCommentFaker(post.Id).Generate();
            comment.Content = null!;

            _repository.AddComment(comment);
        }

        [TestMethod]
        public void DeletePost_CascadeDeleteComments()
        {
            var post = DataGenerator.GetPostFaker().Generate();
            _repository.AddPost(post);

            var comments = DataGenerator
                .GetCommentFaker(post.Id)
                .Generate(3);

            foreach (var comment in comments)
            {
                _repository.AddComment(comment);
            }

            _repository.DeletePost(post.Id);

            var commentsAfterDelete = _repository.GetCommentsByPostId(post.Id);

            Assert.AreEqual(0, commentsAfterDelete.Count());
        }
    }
}
