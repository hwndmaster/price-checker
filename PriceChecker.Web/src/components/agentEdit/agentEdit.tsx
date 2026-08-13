import React, { useEffect, useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { translateErrorsToForm } from "@hwndmaster/atom-react-core";
import { FormDropdown, FormInputText, toastService } from "@hwndmaster/atom-react-prime";
import { LoadingSpinner } from "@hwndmaster/atom-react-redux";
import { Button } from "@/primereact";
import { AgentRef, agentRef } from "@/models/types";
import Agent from "@/models/agent";
import { agentSchema, AgentSchemaData } from "@/schemas/agentSchema";
import * as store from "@/store";
import LoadingTargets from "@/shared/loadingTargets";
import { fetchAgentHandlers } from "@/store/agents/messages";
import styles from "./agentEdit.module.scss";

interface AgentEditProps {
    agentId: AgentRef | null;
    onClose: () => void;
}

const AgentEdit: React.FC<AgentEditProps> = ({ agentId, onClose }) => {
    const dispatch = store.useAppDispatch();
    const agent = store.useAppSelector((state) => agentId !== null ? store.Agents.Selectors.selectAgentById(state, agentId) : undefined);
    const isAddMode = agentId === null;
    const [handlers, setHandlers] = useState<string[]>([]);

    const form = useForm<AgentSchemaData>({
        resolver: zodResolver(agentSchema),
        defaultValues: {
            key: "",
            url: "",
            pricePattern: "",
            handler: "",
            decimalDelimiter: ".",
        }
    });

    useEffect(() => {
        void fetchAgentHandlers().then((fetchedHandlers) => {
            setHandlers(fetchedHandlers);
            if (isAddMode && fetchedHandlers.length > 0) {
                form.resetField("handler", { defaultValue: fetchedHandlers[0] });
            }
        });
    }, [form, isAddMode]);

    useEffect(() => {
        if (!isAddMode && agent !== undefined) {
            form.reset({
                key: agent.key,
                url: agent.url,
                pricePattern: agent.pricePattern,
                handler: agent.handler,
                decimalDelimiter: agent.decimalDelimiter,
            });
        }
    }, [agent, form, isAddMode]);

    const onSubmit = (data: AgentSchemaData): void => {
        const agentData: Agent = {
            id: agent?.id ?? agentRef.default(),
            lastModified: agent?.lastModified ?? 0,
            key: data.key,
            url: data.url,
            pricePattern: data.pricePattern,
            handler: data.handler,
            decimalDelimiter: data.decimalDelimiter,
        };

        form.clearErrors();

        dispatch(store.Agents.Actions.saveAgent(agentData, translateErrorsToForm(form), (savedAgentId: AgentRef | undefined) => {
            if (savedAgentId == null) {
                toastService.showError("Save failed", "An error occurred while saving the agent.");
                return;
            }

            toastService.showSuccess(
                isAddMode ? "Agent added" : "Agent updated",
                "The agent has been successfully " + (isAddMode ? "added." : "updated."));
            onClose();
        }));
    };

    if (!isAddMode && agent === undefined) {
        return <div>Agent not found</div>;
    }

    return (
        <LoadingSpinner target={LoadingTargets.AgentEdit}>
            <form onSubmit={(e) => void form.handleSubmit(onSubmit)(e)} className={styles.form}>
                <div className={`${styles.row} ${styles.firstRow}`}>
                    <FormInputText name="key" form={form} label="Key" data-test_id="AgentEdit__Key_Input" />
                </div>
                <div className={styles.row}>
                    <FormInputText name="url" form={form} label="URL" data-test_id="AgentEdit__Url_Input" />
                </div>
                <div className={styles.row}>
                    <FormInputText name="pricePattern" form={form} label="Price Pattern" data-test_id="AgentEdit__Price_Pattern_Input" />
                </div>
                <div className={styles.row}>
                    <FormDropdown
                        name="handler"
                        form={form}
                        label="Handler"
                        options={handlers}
                        data-test_id="AgentEdit__Handler_Dropdown"
                    />
                </div>
                <div className={styles.row}>
                    <FormInputText name="decimalDelimiter" form={form} label="Decimal Delimiter" data-test_id="AgentEdit__Decimal_Delimiter_Input" />
                </div>

                <div className={styles.actions}>
                    <Button type="submit" label="Save" icon="pi pi-check" data-test_id="AgentEdit__Save_Button" />
                    <Button type="button" label="Cancel" icon="pi pi-times" severity="secondary" onClick={onClose} data-test_id="AgentEdit__Cancel_Button" />
                </div>
            </form>
        </LoadingSpinner>
    );
};

export default AgentEdit;
