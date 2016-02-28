using MoneyManager.Foundation.Model;

namespace MoneyManager.Foundation.Messages
{
    public class CategorySelectedMessage : MvxMessage
    {
        /// <summary>
        ///     Message to notify about a selected category after choosing.
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="selectedCategory">Selected Category</param>
        public CategorySelectedMessage(object sender, Category selectedCategory) : base(sender)
        {
            SelectedCategory = selectedCategory;
        }

        /// <summary>
        ///     Selected Category.
        /// </summary>
        public Category SelectedCategory { get; private set; }
    }
}