import { CompositeTreeNode, TreeNode } from '@theia/core/lib/browser/tree/tree';
import { ExpandableTreeNode } from '@theia/core/lib/browser/tree/tree-expansion';
import { SelectableTreeNode } from '@theia/core/lib/browser/tree/tree-selection';

export interface SearchStudioTreeNode extends SelectableTreeNode {
    readonly kind: string;
    readonly label: string;
    readonly iconClass?: string;
    readonly description?: string;
}

export interface SearchStudioCompositeTreeNode extends SearchStudioTreeNode, ExpandableTreeNode {
    children: SearchStudioTreeNode[];
}

export interface SearchStudioTreeRoot extends SearchStudioCompositeTreeNode {
    readonly kind: 'root';
    readonly visible: false;
}

export interface SearchStudioTreeLeafNodeOptions<TData extends object = object, TKind extends string = string> {
    readonly id: string;
    readonly kind: TKind;
    readonly label: string;
    readonly iconClass?: string;
    readonly description?: string;
    readonly visible?: boolean;
    readonly data?: TData;
}

export interface SearchStudioCompositeTreeNodeOptions<TData extends object = object, TKind extends string = string> extends SearchStudioTreeLeafNodeOptions<TData, TKind> {
    readonly expanded?: boolean;
    readonly children: readonly SearchStudioTreeNode[];
}

export function createSearchStudioTreeRoot(id: string, label: string): SearchStudioTreeRoot {
    return {
        id,
        kind: 'root',
        name: label,
        label,
        visible: false,
        parent: undefined,
        selected: false,
        expanded: true,
        children: []
    };
}

export function createSearchStudioTreeLeafNode<TData extends object = object, TKind extends string = string>(
    options: SearchStudioTreeLeafNodeOptions<TData, TKind>
): SearchStudioTreeNode & TData & { kind: TKind }
{
    const node = {
        id: options.id,
        kind: options.kind,
        name: options.label,
        label: options.label,
        description: options.description,
        icon: options.iconClass,
        iconClass: options.iconClass,
        visible: options.visible,
        parent: undefined,
        selected: false,
        ...(options.data ?? {})
    };

    return node as SearchStudioTreeNode & TData & { kind: TKind };
}

export function createSearchStudioCompositeTreeNode<TData extends object = object, TKind extends string = string>(
    options: SearchStudioCompositeTreeNodeOptions<TData, TKind>
): SearchStudioCompositeTreeNode & TData & { kind: TKind }
{
    const node = {
        ...createSearchStudioTreeLeafNode(options),
        expanded: options.expanded ?? false,
        children: []
    } as SearchStudioCompositeTreeNode & TData & { kind: TKind };

    CompositeTreeNode.addChildren(node, [...options.children]);

    return node;
}

export function isSearchStudioTreeNode(node: unknown): node is SearchStudioTreeNode {
    return TreeNode.is(node)
        && typeof (node as SearchStudioTreeNode).kind === 'string'
        && typeof (node as SearchStudioTreeNode).label === 'string';
}
