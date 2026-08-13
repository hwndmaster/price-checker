import { SagaRunner } from "@hwndmaster/atom-testing-utils";
import { Common } from "@hwndmaster/atom-react-redux";
import * as api from "@/api/api.generated";
import LoadingTargets from "@/shared/loadingTargets";
import { fakeAxios } from "@/store/testUtils/sagas";
import { createProduct } from "@/utils/tests/testModels";
import * as actions from "./actions";
import * as actionsInternal from "./actionsInternal";
import { fetchProductSaga } from "./sagas";

const sagaRunner = new SagaRunner();

beforeEach(() => {
    fakeAxios.reset();
    sagaRunner.reset();
    vi.clearAllMocks();
});

it("fetchProductSaga: resolves the fetched product through the callback instead of the store", async () => {
    // Arrange
    const product = createProduct();
    const apiProduct: api.ProductDto = {
        id: product.id,
        name: product.name,
        category: product.category ?? undefined,
        description: product.description ?? undefined,
        sources: [],
        dateCreated: 1,
        lastModified: product.lastModified,
    };
    fakeAxios.setupGet(api.ProductsClient, "productsGET", { id: product.id }).reply(200, apiProduct);
    const onResolved = vi.fn();

    // Act
    await sagaRunner.runSaga(fetchProductSaga, actions.fetchProduct(product.id, onResolved));

    // Assert
    expect(onResolved).toHaveBeenCalledWith(product);
    expect(sagaRunner.findDispatchedAction(Common.Actions.showLoader)).toBe(LoadingTargets.ProductEdit);
    expect(sagaRunner.findDispatchedAction(Common.Actions.hideLoader)).toBe(LoadingTargets.ProductEdit);
    // The product must not leak into the store: the edit form owns it.
    expect(sagaRunner.findDispatchedAction(actionsInternal.setOverviews)).toBeUndefined();
});
