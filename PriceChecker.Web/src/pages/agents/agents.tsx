import React, { useEffect, useRef, useState } from "react";
import { LoadingSpinner } from "@hwndmaster/atom-react-redux";
import { toastService } from "@hwndmaster/atom-react-prime";
import {
    Button,
    Column,
    confirmDialog,
    DataTable,
    Dialog,
    IconField,
    InputIcon,
    InputText,
} from "@/primereact";
import Agent from "@/models/agent";
import { AgentRef } from "@/models/types";
import LoadingTargets from "@/shared/loadingTargets";
import * as store from "@/store";
import AgentEdit from "@/components/agentEdit/agentEdit";

const Agents: React.FC = () => {
    const dispatch = store.useAppDispatch();
    const agents = store.useAppSelector((state) => state.agents.agents);

    const [globalFilter, setGlobalFilter] = useState("");
    const [editedAgentId, setEditedAgentId] = useState<AgentRef | null>(null);
    const [isEditDialogVisible, setEditDialogVisible] = useState(false);

    const hasFetched = useRef(false);
    useEffect(() => {
        if (hasFetched.current) return;
        hasFetched.current = true;
        dispatch(store.Agents.Actions.fetchAgents());
    }, [dispatch]);

    const openAddDialog = (): void => {
        setEditedAgentId(null);
        setEditDialogVisible(true);
    };

    const openEditDialog = (agent: Agent): void => {
        setEditedAgentId(agent.id);
        setEditDialogVisible(true);
    };

    const deleteAgent = (agent: Agent): void => {
        confirmDialog({
            message: `Are you sure you want to delete the '${agent.key}' agent? The sources of the products which use this agent will be removed as well.`,
            header: "Delete agent",
            icon: "pi pi-exclamation-triangle",
            acceptClassName: "p-button-danger",
            accept: () => {
                dispatch(store.Agents.Actions.deleteAgent(agent.id, () => {
                    toastService.showSuccess("Agent deleted", `The agent '${agent.key}' has been deleted.`);
                }));
            },
        });
    };

    const header = (
        <div className="flex flex-wrap align-items-center justify-content-between gap-2">
            <Button
                label="Add Agent"
                icon="pi pi-plus"
                onClick={openAddDialog}
                data-test_id="Agents__Add_Button"
            />
            <IconField iconPosition="left">
                <InputIcon className="pi pi-search" />
                <InputText
                    placeholder="Search..."
                    value={globalFilter}
                    onChange={(e) => setGlobalFilter(e.target.value)}
                    data-test_id="Agents__Search"
                />
            </IconField>
        </div>
    );

    const actionsTemplate = (agent: Agent): React.ReactNode => (
        <div className="flex gap-1">
            <Button
                icon="pi pi-pencil"
                rounded text
                tooltip="Edit"
                onClick={() => openEditDialog(agent)}
                data-test_id="Agents__Edit_Button"
            />
            <Button
                icon="pi pi-trash"
                rounded text
                severity="danger"
                tooltip="Delete"
                onClick={() => deleteAgent(agent)}
                data-test_id="Agents__Delete_Button"
            />
        </div>
    );

    return (
        <LoadingSpinner target={LoadingTargets.Agents}>
            <DataTable
                value={agents}
                dataKey="id"
                header={header}
                globalFilter={globalFilter}
                globalFilterFields={["key", "url", "handler"]}
                sortMode="single"
                sortField="key"
                sortOrder={1}
                emptyMessage="No agents yet. Add your first scanning agent."
                data-test_id="Agents__Table"
            >
                <Column field="key" header="Key" sortable data-test_id="Agents__Key" />
                <Column field="url" header="URL" sortable />
                <Column field="handler" header="Handler" sortable style={{ width: "16rem" }} />
                <Column field="decimalDelimiter" header="Delimiter" style={{ width: "8rem", textAlign: "center" }} />
                <Column body={actionsTemplate} style={{ width: "8rem" }} />
            </DataTable>

            <Dialog
                header={editedAgentId == null ? "Add Agent" : "Edit Agent"}
                visible={isEditDialogVisible}
                style={{ width: "36rem" }}
                onHide={() => setEditDialogVisible(false)}
                data-test_id="Agents__Edit_Dialog"
            >
                <AgentEdit
                    agentId={editedAgentId}
                    onClose={() => setEditDialogVisible(false)}
                />
            </Dialog>
        </LoadingSpinner>
    );
};

export default Agents;
