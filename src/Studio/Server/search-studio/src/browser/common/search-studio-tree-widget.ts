import { ContextMenuRenderer } from '@theia/core/lib/browser/context-menu-renderer';
import { MenuPath } from '@theia/core/lib/common/menu';
import { TreeNode } from '@theia/core/lib/browser/tree/tree';
import { TreeModel } from '@theia/core/lib/browser/tree/tree-model';
import { SelectableTreeNode, TreeSelection } from '@theia/core/lib/browser/tree/tree-selection';
import { NodeProps, TreeProps, TreeWidget } from '@theia/core/lib/browser/tree/tree-widget';
import * as React from '@theia/core/shared/react';
import { isSearchStudioTreeNode } from './search-studio-tree-types';

export abstract class SearchStudioTreeWidget extends TreeWidget {

    protected constructor(
        props: TreeProps,
        model: TreeModel,
        contextMenuRenderer: ContextMenuRenderer
    )
    {
        super(props, model, contextMenuRenderer);
    }

    protected override toNodeIcon(node: TreeNode): string {
        if (isSearchStudioTreeNode(node) && node.iconClass) {
            return node.iconClass;
        }

        return super.toNodeIcon(node);
    }

    protected override toNodeName(node: TreeNode): string {
        if (isSearchStudioTreeNode(node)) {
            return node.label;
        }

        return super.toNodeName(node);
    }

    protected override toNodeDescription(_node: TreeNode): string {
        return '';
    }

    protected override renderIcon(node: TreeNode, props: NodeProps): React.ReactNode {
        const iconClass = this.toNodeIcon(node);

        if (iconClass) {
            return React.createElement('div', { className: iconClass });
        }

        return super.renderIcon(node, props);
    }

    protected getContextMenuPath(node: SelectableTreeNode): MenuPath | undefined {
        return this.props.contextMenuPath;
    }

    protected override handleContextMenuEvent(node: object | undefined, event: React.MouseEvent<HTMLElement>): void {
        if (SelectableTreeNode.is(node)) {
            if (!this.props.multiSelect || !node.selected) {
                const type = !!this.props.multiSelect && this.hasCtrlCmdMask(event)
                    ? TreeSelection.SelectionType.TOGGLE
                    : TreeSelection.SelectionType.DEFAULT;
                this.model.addSelection({ node, type });
            }

            this.focusService.setFocus(node);

            const contextMenuPath = this.getContextMenuPath(node);

            if (contextMenuPath) {
                const { x, y } = event.nativeEvent;
                const args = this.toContextMenuArgs(node);
                const target = event.currentTarget;

                setTimeout(() => this.contextMenuRenderer.render({
                    menuPath: contextMenuPath,
                    context: target,
                    anchor: { x, y },
                    args
                }), 10);
            }
        }

        event.stopPropagation();
        event.preventDefault();
    }
}
