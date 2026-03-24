import { ExpandableTreeNode } from '@theia/core/lib/browser/tree/tree-expansion';
import { SearchStudioCompositeTreeNode, SearchStudioTreeRoot } from './search-studio-tree-types';

export function synchronizeTopLevelExpansionState(
    root: SearchStudioTreeRoot,
    expansionState: Map<string, boolean>
): void
{
    const topLevelNodes = root.children.filter(
        (node): node is SearchStudioCompositeTreeNode => ExpandableTreeNode.is(node)
    );

    if (topLevelNodes.length === 0) {
        expansionState.clear();
        return;
    }

    const hasStoredState = topLevelNodes.some(node => expansionState.has(node.id));

    if (!hasStoredState) {
        topLevelNodes[0].expanded = true;

        for (const node of topLevelNodes.slice(1)) {
            node.expanded = false;
        }
    } else {
        for (const node of topLevelNodes) {
            node.expanded = expansionState.get(node.id) ?? false;
        }
    }

    expansionState.clear();

    for (const node of topLevelNodes) {
        expansionState.set(node.id, node.expanded);
    }
}

export function rememberTopLevelExpansionState(
    root: SearchStudioTreeRoot | undefined,
    node: Readonly<ExpandableTreeNode>,
    expansionState: Map<string, boolean>
): boolean
{
    if (!root || node.parent?.id !== root.id) {
        return false;
    }

    expansionState.set(node.id, node.expanded);
    return true;
}
