// ---------------------------------------------------------------------------
// Red-Black tree code (based on C version of "rbtree" by Franck Bui-Huu
// https://github.com/fbuihuu/libtree/blob/master/rb.c

namespace Voronoi
{
    public class RBNode
    {
    }

    public class RBNodeBase : RBNode
    {
        public RBNodeBase Parent;
        public RBNodeBase Prev;
        public RBNodeBase Next;
        public RBNodeBase Left;
        public RBNodeBase Right;
        public bool Red;
        public bool Black { get { return !Red; } }

        public RBNodeBase()
        {
            this.Parent = null;
            this.Prev = null;
            this.Next = null;
            this.Left = null;
            this.Right = null;
            this.Red = false;
        }

        public static implicit operator bool(RBNodeBase a)
        {
            return a != null;
        }
    }

    public class RBTree :  RBNodeBase
    {
        public RBNodeBase Root;

        public RBTree()
        {
            this.Root = null;
        }

        public void Insert(RBNodeBase node, RBNodeBase successor)
        {
            RBNodeBase parent = null;
            if (node)
            {
                // >>> rhill 2011-05-27: Performance: cache previous/next nodes
                successor.Prev = node;
                successor.Next = node.Next;
                if (node.Next)
                {
                    node.Next.Prev = successor;
                }
                node.Next = successor;
                // <<<
                if (node.Right)
                {
                    // in-place expansion of node.rbRight.getFirst();
                    node = node.Right;
                    while (node.Left)
                        node = node.Left;
                    node.Left = successor;
                }
                else
                {
                    node.Right = successor;
                }
                parent = node;
            }
            // rhill 2011-06-07: if node is null, successor must be inserted
            // to the left-most part of the tree
            else if (Root)
            {
                node = GetFirst(Root);
                // >>> Performance: cache previous/next nodes
                successor.Prev = null;
                successor.Next = node;
                node.Prev = successor;
                // <<<
                node.Left = successor;
                parent = node;
            }
            else
            {
                // >>> Performance: cache previous/next nodes
                successor.Prev = null;
                successor.Next = null;
                // <<<
                Root = successor;
                parent = null;
            }

            successor.Left = null;
            successor.Right = null;
            successor.Parent = parent;
            successor.Red = true;

            // Fixup the modified tree by recoloring nodes and performing
            // rotations (2 at most) hence the red-black tree properties are
            // preserved.
            RBNodeBase grandpa;
            RBNodeBase uncle;
            node = successor;
            while (parent != null && parent.Red)
            {
                grandpa = parent.Parent;
                if (parent == grandpa.Left)
                {
                    uncle = grandpa.Right;
                    if (uncle != null && uncle.Red)
                    {
                        parent.Red = false;
                        uncle.Red = false;
                        grandpa.Red = true;
                        node = grandpa;
                    }
                    else
                    {
                        if (node == parent.Right)
                        {
                            RotateLeft(parent);
                            node = parent;
                            parent = node.Parent;
                        }
                        parent.Red = false;
                        grandpa.Red = true;
                        RotateRight(grandpa);
                    }
                }
                else
                {
                    uncle = grandpa.Left;
                    if (uncle != null && uncle.Red)
                    {
                        parent.Red = false;
                        uncle.Red = false;
                        grandpa.Red = true;
                        node = grandpa;
                    }
                    else
                    {
                        if (node == parent.Left)
                        {
                            RotateRight(parent);
                            node = parent;
                            parent = node.Parent;
                        }
                        parent.Red = false;
                        grandpa.Red = true;
                        RotateLeft(grandpa);
                    }
                }
                parent = node.Parent;
            }
            Root.Red = false;
        }

        public void Remove(RBNodeBase node)
        {
            // >>> rhill 2011-05-27: Performance: cache previous/next nodes
            if (node.Next)
            {
                node.Next.Prev = node.Prev;
            }
            if (node.Prev)
            {
                node.Prev.Next = node.Next;
            }
            node.Next = null;
            node.Prev = null;
            // <<<

            RBNodeBase parent = node.Parent;
            RBNodeBase left = node.Left;
            RBNodeBase right = node.Right;
            RBNodeBase next = (left == null) ? right : (right == null) ? left : GetFirst(right);

            if (parent)
            {
                if (parent.Left == node)
                    parent.Left = next;
                else
                    parent.Right = next;
            }
            else
            {
                Root = next;
            }

            //	rhill - enforce red-black rules
            bool isRed;
            if (left && right)
            {
                isRed = next.Red;
                next.Red = node.Red;
                next.Left = left;
                left.Parent = next;
                if (next != right)
                {
                    parent = next.Parent;
                    next.Parent = node.Parent;
                    node = next.Right;
                    parent.Left = node;
                    next.Right = right;
                    right.Parent = next;
                }
                else
                {
                    next.Parent = parent;
                    parent = next;
                    node = next.Right;
                }
            }
            else
            {
                isRed = node.Red;
                node = next;
            }
            // 'node' is now the sole successor's child and 'parent' its
            // new parent (since the successor can have been moved)
            if (node)
            {
                node.Parent = parent;
            }
            // the 'easy' cases
            if (isRed)
            {
                return;
            }
            if (node != null && node.Red)
            {
                node.Red = false;
                return;
            }
            // the other cases
            RBNodeBase sibling;
            do
            {
                if (node == Root)
                    break;
                if (node == parent.Left)
                {
                    sibling = parent.Right;
                    if (sibling.Red)
                    {
                        sibling.Red = false;
                        parent.Red = true;
                        RotateLeft(parent);
                        sibling = parent.Right;
                    }
                    if ((sibling.Left != null && sibling.Left.Red) ||
                        (sibling.Right != null && sibling.Right.Red))
                    {
                        if (sibling.Right == null || !sibling.Right.Red)
                        {
                            sibling.Left.Red = false;
                            sibling.Red = true;
                            RotateRight(sibling);
                            sibling = parent.Right;
                        }
                        sibling.Red = parent.Red;
                        parent.Red = false;
                        sibling.Right.Red = false;
                        RotateLeft(parent);
                        node = Root;
                        break;
                    }
                }
                else
                {
                    sibling = parent.Left;
                    if (sibling.Red)
                    {
                        sibling.Red = false;
                        parent.Red = true;
                        RotateRight(parent);
                        sibling = parent.Left;
                    }
                    if ((sibling.Left != null && sibling.Left.Red) ||
                        (sibling.Right != null && sibling.Right.Red))
                    {
                        if (sibling.Left == null || !sibling.Left.Red)
                        {
                            sibling.Right.Red = false;
                            sibling.Red = true;
                            RotateLeft(sibling);
                            sibling = parent.Left;
                        }
                        sibling.Red = parent.Red;
                        parent.Red = false;
                        sibling.Left.Red = false;
                        RotateRight(parent);
                        node = Root;
                        break;
                    }
                }
                sibling.Red = true;
                node = parent;
                parent = parent.Parent;
            }
            while (!node.Red);

            if (node)
                node.Red = false;
        }

        private void RotateLeft(RBNodeBase node)
        {
            RBNodeBase p = node;
            RBNodeBase q = node.Right;
            RBNodeBase parent = p.Parent;
            if (parent)
            {
                if (parent.Left == p)
                    parent.Left = q;
                else
                    parent.Right = q;
            }
            else
            {
                Root = q;
            }
            q.Parent = parent;
            p.Parent = q;
            p.Right = q.Left;
            if (p.Right)
            {
                p.Right.Parent = p;
            }
            q.Left = p;
        }

        private void RotateRight(RBNodeBase node)
        {
            RBNodeBase p = node;
            RBNodeBase q = node.Left;
            RBNodeBase parent = p.Parent;
            if (parent)
            {
                if (parent.Left == p)
                    parent.Left = q;
                else
                    parent.Right = q;
            }
            else
            {
                Root = q;
            }
            q.Parent = parent;
            p.Parent = q;
            p.Left = q.Right;
            if (p.Left)
            {
                p.Left.Parent = p;
            }
            q.Right = p;
        }

        public RBNodeBase GetFirst(RBNodeBase node)
        {
            while (node.Left)
                node = node.Left;
            return node;
        }

        public RBNodeBase GetLast(RBNodeBase node)
        {
            while (node.Right)
                node = node.Right;
            return node;
        }

        public static implicit operator bool(RBTree a)
        {
            return a != null;
        }
    }
}