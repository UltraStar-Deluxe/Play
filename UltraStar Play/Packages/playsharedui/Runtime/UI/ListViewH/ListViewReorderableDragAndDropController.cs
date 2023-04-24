// public class ListViewReorderableDragAndDropController : BaseReorderableDragAndDropController
// {
//     protected readonly BaseListViewH m_ListView;
//
//     public ListViewReorderableDragAndDropController(BaseListViewH view)
//         : base((BaseHorizontalCollectionView) view)
//     {
//         this.m_ListView = view;
//     }
//
//     public override DragVisualMode HandleDragAndDrop(IListDragAndDropArgs args) => args.dragAndDropPosition == DragAndDropPosition.OverItem || !this.enableReordering ? DragVisualMode.Rejected : (args.dragAndDropData.userData == this.m_ListView ? DragVisualMode.Move : DragVisualMode.Rejected);
//
//     public override void OnDrop(IListDragAndDropArgs args)
//     {
//         int insertAtIndex = args.insertAtIndex;
//         int num1 = 0;
//         int num2 = 0;
//         for (int index = this.m_SelectedIndices.Count - 1; index >= 0; --index)
//         {
//             int selectedIndex = this.m_SelectedIndices[index];
//             if (selectedIndex >= 0)
//             {
//                 int newIndex = insertAtIndex - num1;
//                 if (selectedIndex > insertAtIndex)
//                 {
//                     selectedIndex += num2;
//                     ++num2;
//                 }
//                 else if (selectedIndex < newIndex)
//                 {
//                     ++num1;
//                     --newIndex;
//                 }
//                 this.m_ListView.viewController.Move(selectedIndex, newIndex);
//             }
//         }
//         if (this.m_ListView.selectionType != 0)
//         {
//             List<int> indices = new List<int>();
//             for (int index = 0; index < this.m_SelectedIndices.Count; ++index)
//                 indices.Add(insertAtIndex - num1 + index);
//             this.m_ListView.SetSelectionWithoutNotify((IEnumerable<int>) indices);
//         }
//         else
//             this.m_ListView.ClearSelection();
//     }
// }
